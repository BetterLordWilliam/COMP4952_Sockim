// Function to handle the actual scrolling
window.scrollToElementBottom = (element) => {
    // 'element' here is the DOM object passed from Blazor
    if (element) {
        // Scroll the element's scroll height to the bottom
        element.scrollTop = element.scrollHeight;
    }
};
