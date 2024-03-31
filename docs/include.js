document.addEventListener("DOMContentLoaded", function() {
    fetch("header.html")
    .then(response => response.text())
    .then(data => {
        document.querySelector("header").innerHTML = data;
    })
    .catch(error => console.error('Error fetching header:', error));

    fetch("head.html")
    .then(response => response.text())
    .then(data => {
        document.querySelector("head").innerHTML = data;
    })
    .catch(error => console.error('Error fetching head:', error));

    fetch("sidebar.html")
    .then(response => response.text())
    .then(data => {
        document.querySelector("aside").innerHTML = data;
    })
    .catch(error => console.error('Error fetching sidebar:', error));
});
