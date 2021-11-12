const admin = () => {
    // const getUrl = 'https://github.com/patrickdemooij9/MachineLearning/blob/front-end/src/font-end/assets/data/images.json';
    const getUrl = './assets/data/images.json';
    const jsPendingImages = document.querySelector('.js-pending-images');

    jsPendingImages.innerHTML = null;

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
}

const urlPost = '';

const ok = (e) => {
    const wrapper = e.target.parentNode.closest('.pending-image')
    console.log('ok: ', wrapper);

    const img = wrapper.querySelector('img');

    const data = {
        image: img.getAttribute('src'),
        id: parseInt(wrapper.id),
        accepted: true,
    }

    isFetchAccepted(data)
}
const notOk = (e) => {
    const wrapper = e.target.parentNode.closest('.pending-image')
    console.log('notOk: ', wrapper)
    const img = wrapper.querySelector('img');

    const data = {
        image: img.getAttribute('src'),
        id: parseInt(wrapper.id),
        accepted: false,
    }

    isFetchAccepted(data)
}

const isFetchAccepted = (data) => {
    console.log(data)

    fetch(urlPost, {method: 'post', body: JSON.stringify(data)})
    admin();
}

admin();