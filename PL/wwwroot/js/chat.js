const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveMessage", (senderUserId, message) => {
    console.log(`Message from ${senderUserId}: ${message}`);
       var li = document.createElement("li");
       document.getElementById("messagesList").appendChild(li);
       li.textContent = `${senderUserId} says ${message}`; //HAS to be assigned to non-markup or else XSS
});

connection.start().then(() => {
    console.log("Connection Started!");  
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    event.preventDefault();
    const recipientUserId = document.getElementById("userInput").value;
    const message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", recipientUserId, message).catch(function (err) {
        return console.error(err.toString());
    });

    document.getElementById("messageInput").value = ""; 
    document.getElementById("userInput").value = "";
    document.getElementById("messageInput").focus();
});
