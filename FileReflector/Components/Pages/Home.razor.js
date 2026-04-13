function scrollToBottom() {
    console.log("called")
    var domElement = document.getElementById('rsync-body-div')
    domElement.scrollTo({
        top: domElement.scrollHeight,
        behavior: 'smooth'
    })
}