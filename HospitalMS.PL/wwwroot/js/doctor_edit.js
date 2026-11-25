const bio = document.getElementById("Bio");
const counter = document.getElementById("bioCounter");

function updateBioCounter() {
    const length = bio.value.length;
    counter.textContent = length === 1000
        ? "You have reached the maximum of 1000 characters."
        : `${length} / 1000 characters`;
}

bio.addEventListener("input", updateBioCounter);
updateBioCounter();