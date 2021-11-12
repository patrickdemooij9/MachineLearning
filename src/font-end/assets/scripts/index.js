const upload = () => {
    const form = document.querySelector('.js-upload-form');
    const image = document.querySelector('.js-image-preview');
    const fileInput = document.querySelector('.js-upload-file');
    const uploadBtn = document.querySelector('.js-upload');
    const filename = document.querySelector('.js-filename');
    const postUrl = '';

    uploadBtn.setAttribute('disabled', true)

    fileInput.addEventListener("change", (ev) => {
        let img = new Image();
        const files = ev.target.files;
        const file = files[0];

        if (file.type.match('image.*')) {
            var reader = new FileReader();

            reader.readAsDataURL(file);
            reader.onload = function (evt) {
                if (evt.target.readyState == FileReader.DONE) {
                    const imageSrc = evt.target.result;
                    console.log(evt.target.result);

                    fetch(postUrl, { 
                        method: 'POST',
                        body: {
                            image: imageSrc,
                        }
                    }).then((res) => {
                        const debug = true;
                        if(res.accepted || debug) {
                            image.src = evt.target.result;
                            uploadBtn.removeAttribute('disabled', true)
                            filename.innerHTML = 'Screen Shot 2017-07-29 at 15.54.25.png';
                        } else {
                            image.classList.add('not-accepted');
                        }
                    })
                }
            }
        }
    });
}

upload();