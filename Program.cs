using ChessDotNet;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChessDotNet.Pieces;
using System.Linq;

// Data structure for chess moves
public class ChessMove
{
    public string From { get; set; }
    public string To { get; set; }
    public string FEN { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("1. Host game");
        Console.WriteLine("2. Join game");
        string choice = Console.ReadLine();

        if (choice == "1")
        {
            var server = new ChessServer();
            await server.StartServer();
        }
        else if (choice == "2")
        {
            Console.Write("Enter server IP: ");
            string serverIP = Console.ReadLine();
            var client = new ChessClient();
            await client.ConnectToGame(serverIP);
        }
    }
}
