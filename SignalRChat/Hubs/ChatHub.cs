using System;
using System.Web;
using Microsoft.AspNet.SignalR;
namespace DominoesWithCompadres
{
    public class ChatHub : Hub
    {
        public void Send(string name, string message)
        {
            // Call the addNewMessageToPage method to update clients.

            Groups.Add(Context.ConnectionId, "1");

            Clients.Group("1").addNewMessageToPage(name, message);
        }

        public void JoinGame(string name)
        {
            Clients.All.addNewMessageToPage(name + "2");
        }

        public void SitAtTable(string name)
        {
            Clients.All.addNewMessageToPage(name + "3");
        }
    }
}