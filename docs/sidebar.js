document.addEventListener("DOMContentLoaded", function() {
    fetch("sidebar.html")
    .then(response => response.text())
    .then(data => {
        document.querySelector("aside").innerHTML = data;
    })
    .catch(error => console.error('Error fetching sidebar:', error));
});
