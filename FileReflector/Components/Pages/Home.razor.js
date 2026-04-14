function scrollToBottom() {
    setTimeout(() => {
        var domElement = document.getElementById('rsync-log-window');
        if (!domElement) return;
        domElement.scrollTo({
            top: domElement.scrollHeight,
            behavior: 'smooth'
        })
    }, 200);
}