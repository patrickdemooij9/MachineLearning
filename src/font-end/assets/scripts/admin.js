const admin = () => {
    // const getUrl = 'https://github.com/patrickdemooij9/MachineLearning/blob/front-end/src/font-end/assets/data/images.json';
    const getUrl = './assets/data/images.json';
    const jsPendingImages = document.querySelector('.js-pending-images');

    
    var xhttpGet = new XMLHttpRequest();
    xhttpGet.onreadystatechange = function() {
        if (this.readyState == 4 && this.status == 200) {
        

        console.log(JSON.parse(this.response).images)
        JSON.parse(this.response).images.forEach(r => {
            const str = document.createElement("div").innerHTML=`
            <div class="pending-image" id="${r.id}">
                <figure class="image is-128x128">
                    <img src="${r.url}" alt=""/>
                </figure>
                <div class="image-controls">
                    <button onclick="notOk(event)" class="button js-is-notok is-small is-danger is-outlined notok">x</button>
                    <button onclick="ok(event)" class="button js-is-ok is-small is-success is-outlined ok">v</button>
                </div>
            </div>
            `
            jsPendingImages.innerHTML += (str)
        });

        }
    };
    xhttpGet.open("GET", getUrl, true);
    xhttpGet.send();

    
/*
    fetch(getUrl, {mode: 'cors'})
    .then(response => response.json())
    .then(data => {
        data.images.forEach(r => {
            jsPendingImages.innerHTML = `
            <div class="pending-image" id="${r.id}">
                <figure class="image is-128x128">
                    <img src="${r.src}" alt=""/>
                </figure>
                <div class="image-controls">
                    <button class="button js-is-notok is-small is-danger is-outlined notok">x</button>
                    <button class="button js-is-ok is-small is-success is-outlined ok">v</button>
                </div>
            </div>
            `
        })
    });

*/

}

const ok = (e) => {
    const wrapper = e.target.parentNode.closest('.pending-image')

    console.log('ok: ', wrapper)
}
const notOk = (e) => {
    const wrapper = e.target.parentNode.closest('.pending-image')
    console.log('notOk: ', wrapper)
}

admin();