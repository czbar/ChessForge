document.addEventListener("DOMContentLoaded", function () {

    const headerContainer = document.querySelector('header');

    fetch("header.html")
        .then(response => response.text())
        .then(htmlContent => {
            const parser = new DOMParser();
            const navDoc = parser.parseFromString(htmlContent, 'text/html');
            const navLinks = navDoc.querySelectorAll('.nav-link'); // select all links with class "nav-link"

            // get the current page filename
            currentPage = window.location.pathname.split('/').pop();
            if (currentPage == "") {
                currentPage = "index.html";
            }

            // add 'active' class to the link for the current page  
            navLinks.forEach(link => {
                if (link.href.endsWith(currentPage)) {
                    link.classList.add('active');
                }
            });

            headerContainer.outerHTML = navDoc.querySelector('header').outerHTML; // inject modified nav content
        })
        .catch(error => console.error('Error fetching header:', error));

    fetch("sidebar.html")
        .then(response => response.text())
        .then(data => {
            document.querySelector("aside").innerHTML = data;
        })
        .catch(error => console.error('Error fetching sidebar:', error));
});
