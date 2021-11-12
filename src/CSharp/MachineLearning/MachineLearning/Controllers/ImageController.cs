using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace MachineLearning.Controllers
{
    [Route("[controller]/[action]")]
    public class ImageController : Controller
    {
        private static readonly string _dataPath = Path.Combine(Environment.CurrentDirectory, "data");
        private static readonly string _tempPath = Path.Combine(Environment.CurrentDirectory, "temp");
        private static readonly string _inceptionTensorFlowModel = Path.Combine(Environment.CurrentDirectory, "inception/tensorflow_inception_graph.pb");

        private readonly float Threshold = 0.95f;

        [HttpPost]
        public async Task<IActionResult> TestImage()
        {
            var mlContext = new MLContext();
            //Define DataViewSchema for data preparation pipeline and trained model
            DataViewSchema modelSchema;

            // Load trained model
            ITransformer trainedModel = mlContext.Model.Load("model.zip", out modelSchema);

            foreach (var formFile in Request.Form.Files)
            {
                if (formFile.Length > 0)
                {
                    var filePath = Path.GetTempFileName();

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                    var prediction = ClassifySingleImage(mlContext, trainedModel, filePath);
                    if (prediction.Score.Max() <= Threshold)
                    {
                        var pendingPath = Path.Combine(Environment.CurrentDirectory, "pending");
                        if (!Directory.Exists(pendingPath))
                            Directory.CreateDirectory(pendingPath);

                        var labelPath = Path.Combine(pendingPath, prediction.PredictedLabelValue);
                        if (!Directory.Exists(labelPath))
                            Directory.CreateDirectory(labelPath);

                        using (var stream = System.IO.File.Create(Path.Combine(labelPath, formFile.FileName)))
                        {
                            await formFile.CopyToAsync(stream);
                        }
                    }
                }
            }

            return Ok();
        }

        [HttpGet]
        public IActionResult GetPendingItems()
        {
            var pendingItems = new List<PendingItem>();
            var pendingPath = Path.Combine(Environment.CurrentDirectory, "pending");
            if (!Directory.Exists(pendingPath))
                Directory.CreateDirectory(pendingPath);

            foreach(var directory in Directory.GetDirectories(pendingPath))
            {
                foreach(var file in Directory.GetFiles(directory))
                {
                    pendingItems.Add(new PendingItem
                    {
                        Id = file,
                        Url = file,
                        Label = Path.GetFileNameWithoutExtension(directory)
                    });
                }
            }
            return Ok(pendingItems);
        }

        [HttpPost]
        public IActionResult LabelItem(string imageId, string label)
        {
            if (!System.IO.File.Exists(imageId))
                return BadRequest();

            var labelPath = Path.Combine(_dataPath, label);
            if (!Directory.Exists(labelPath))
                Directory.CreateDirectory(labelPath);

            System.IO.File.Move(imageId, Path.Combine(labelPath, Path.GetFileName(imageId)));

            return Ok();
        }

        [HttpPost]
        public IActionResult GenerateModel()
        {
            var mlContext = new MLContext();

            var test = GetImagesForTraining().GroupBy(it => it.Label).ToList();
            var data = mlContext.Data.LoadFromEnumerable(GetImagesForTraining());
            var model = GenerateModel(mlContext, data);
            mlContext.Model.Save(model, data.Schema, "model.zip");
            return Ok();
        }

        private ITransformer GenerateModel(MLContext mlContext, IDataView data)
        {
            IEstimator<ITransformer> pipeline = mlContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: _tempPath, inputColumnName: nameof(ImageData.ImagePath))
                        // The image transforms transform the images into the model's expected format.
                        .Append(mlContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: InceptionSettings.ImageWidth, imageHeight: InceptionSettings.ImageHeight, inputColumnName: "input"))
                        .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: InceptionSettings.ChannelsLast, offsetImage: InceptionSettings.Mean))
                        .Append(mlContext.Model.LoadTensorFlowModel(_inceptionTensorFlowModel)
                        .ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                        .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                        .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                        .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                        .AppendCacheCheckpoint(mlContext);

            Console.WriteLine("Starting training");
            ITransformer model = pipeline.Fit(data);
            Console.WriteLine("Training finished");

            /*Console.WriteLine("Starting test");
            var testDataImages = GetImagesForTest();
            IDataView testData = mlContext.Data.LoadFromEnumerable<ImageData>(testDataImages);
            IDataView predictions = model.Transform(testData);

            // Create an IEnumerable for the predictions for displaying results
            IEnumerable<ImagePrediction> imagePredictionData = mlContext.Data.CreateEnumerable<ImagePrediction>(predictions, true);
            DisplayResults(imagePredictionData);
            MulticlassClassificationMetrics metrics =
            mlContext.MulticlassClassification.Evaluate(predictions,
              labelColumnName: "LabelKey",
              predictedLabelColumnName: "PredictedLabel");

            Console.WriteLine($"LogLoss is: {metrics.LogLoss}");
            Console.WriteLine($"PerClassLogLoss is: {String.Join(" , ", metrics.PerClassLogLoss.Select(c => c.ToString()))}");
            Console.WriteLine("Finished test");*/
            return model;
        }

        ImagePrediction ClassifySingleImage(MLContext mlContext, ITransformer model, string imagePath)
        {
            var imageData = new ImageData()
            {
                ImagePath = imagePath
            };

            // Make prediction function (input = ImageData, output = ImagePrediction)
            var predictor = mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
            var prediction = predictor.Predict(imageData);

            Console.WriteLine($"Image: {Path.GetFileName(imageData.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max()} ");
            return prediction;
        }

        void DisplayResults(IEnumerable<ImagePrediction> imagePredictionData)
        {
            foreach (ImagePrediction prediction in imagePredictionData)
            {
                Console.WriteLine($"Image: {Path.GetFileName(prediction.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max()} ");
            }
        }

        IEnumerable<ImageData> GetImagesForTraining()
        {
            foreach (var folder in Directory.GetDirectories(_dataPath))
            {
                foreach (var file in Directory.GetFiles(folder))
                {
                    yield return new ImageData
                    {
                        ImagePath = file,
                        Label = Path.GetFileNameWithoutExtension(folder)
                    };
                }
            }
        }

        IEnumerable<ImageData> GetImagesForTest()
        {
            foreach (var file in Directory.GetFiles(_tempPath))
            {
                yield return new ImageData
                {
                    ImagePath = file
                };
            }
        }

        public class ImageData
        {
            [LoadColumn(0)]
            public string ImagePath;

            [LoadColumn(1)]
            public string Label;
        }

        public class ImagePrediction : ImageData
        {
            public float[] Score;

            public string PredictedLabelValue;
        }

        struct InceptionSettings
        {
            public const int ImageHeight = 224;
            public const int ImageWidth = 224;
            public const float Mean = 117;
            public const float Scale = 1;
            public const bool ChannelsLast = true;
        }

        public class PendingItem
        {
            public string Id { get; set; }
            public string Url { get; set; }
            public string Label { get; set; }
        }
    }
}
