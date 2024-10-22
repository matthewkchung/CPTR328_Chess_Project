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

public class ChessServer
{
    private TcpListener server;
    private ChessGame game;
    private const int PORT = 5000;

    public ChessServer()
    {
        game = new ChessGame();
    }

    private bool TryMakeMove(string from, string to, Player player)
    {
        try
        {
            // Convert string positions to Position objects
            Position originalPosition = new Position(from);
            Position newPosition = new Position(to);

            // Create the move
            Move move = new Move(originalPosition, newPosition, player);

            // Check if move is valid
            if (game.IsValidMove(move))
            {
                game.MakeMove(move, true);
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task StartServer()
    {
        server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        Console.WriteLine("Server started. Waiting for opponent...");
        PrintBoard();

        using (TcpClient client = await server.AcceptTcpClientAsync())
        using (NetworkStream stream = client.GetStream())
        {
            Console.WriteLine("Opponent connected!");

            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string moveData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ChessMove move = JsonConvert.DeserializeObject<ChessMove>(moveData);

                    // Attempt to make move
                    if (TryMakeMove(move.From, move.To, game.WhoseTurn))
                    {
                        Console.WriteLine($"Move made: {move.From} to {move.To}");
                        PrintBoard();
                    }
                    else
                    {
                        Console.WriteLine("Invalid move!");
                    }

                    // Send updated game state
                    ChessMove response = new ChessMove
                    {
                        FEN = game.GetFen()
                    };

                    string responseData = JsonConvert.SerializeObject(response);
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(responseData);
                    await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }
        }
    }

    private void PrintBoard()
    {
        var board = game.GetBoard();
        Console.WriteLine("   A B C D E F G H");
        Console.WriteLine("   ---------------");
        for (int rank = 7; rank >= 0; rank--)
        {
            Console.Write($"{rank + 1}| ");
            for (int file = 0; file < 8; file++)
            {
                var piece = board[file][rank]; // Adjusting the indexing based on how GetBoard() is structured
                char pieceChar = '.';

                if (piece != null)
                {
                    // Get the appropriate character based on piece type
                    if (piece is Pawn) pieceChar = 'P';
                    else if (piece is Rook) pieceChar = 'R';
                    else if (piece is Knight) pieceChar = 'N';
                    else if (piece is Bishop) pieceChar = 'B';
                    else if (piece is Queen) pieceChar = 'Q';
                    else if (piece is King) pieceChar = 'K';

                    // You can add further checks here if you have some way to determine the color of the piece,
                    // but since piece.Owner is not working, you might want to assume a default color.
                }
                Console.Write($"{pieceChar} ");
            }
            Console.WriteLine();
        }
    }
}

// Similar changes would be needed in the ChessClient class
public class ChessClient
{
    private TcpClient client;
    private ChessGame game;

    public ChessClient()
    {
        game = new ChessGame();
    }

    private bool TryMakeMove(string from, string to, Player player)
    {
        try
        {
            Position originalPosition = new Position(from);
            Position newPosition = new Position(to);
            Move move = new Move(originalPosition, newPosition, player);

            if (game.IsValidMove(move))
            {
                game.MakeMove(move, true);
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task ConnectToGame(string serverIP)
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, 5000);
            Console.WriteLine("Connected to server!");
            PrintBoard();

            using (NetworkStream stream = client.GetStream())
            {
                while (true)
                {
                    // Get move from player
                    Console.Write("Enter move (e.g., e2 e4): ");
                    string moveInput = Console.ReadLine();

                    string[] moveParts = moveInput.Split(' ');
                    if (moveParts.Length != 2)
                    {
                        Console.WriteLine("Invalid move format. Use 'from to' (e.g., 'e2 e4')");
                        continue;
                    }

                    if (TryMakeMove(moveParts[0], moveParts[1], game.WhoseTurn))
                    {
                        ChessMove move = new ChessMove
                        {
                            From = moveParts[0],
                            To = moveParts[1]
                        };

                        // Send move to server
                        string moveData = JsonConvert.SerializeObject(move);
                        byte[] buffer = Encoding.UTF8.GetBytes(moveData);
                        await stream.WriteAsync(buffer, 0, buffer.Length);

                        // Receive response
                        buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string responseData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ChessMove response = JsonConvert.DeserializeObject<ChessMove>(responseData);

                        // Update local game state
                        game = new ChessGame(response.FEN);
                        PrintBoard();
                    }
                    else
                    {
                        Console.WriteLine("Invalid move!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    private void PrintBoard()
    {
        var board = game.GetBoard();
        Console.WriteLine("   A B C D E F G H");
        Console.WriteLine("   ---------------");
        for (int rank = 7; rank >= 0; rank--)
        {
            Console.Write($"{rank + 1}| ");
            for (int file = 0; file < 8; file++)
            {
                int index = (rank * 8) + file;
                var piece = board[file][rank]; // Adjusting the indexing based on how GetBoard() is structured
                char pieceChar = '.';

                if (piece != null)
                {
                    // Get the appropriate character based on piece type
                    if (piece is Pawn) pieceChar = 'P';
                    else if (piece is Rook) pieceChar = 'R';
                    else if (piece is Knight) pieceChar = 'N';
                    else if (piece is Bishop) pieceChar = 'B';
                    else if (piece is Queen) pieceChar = 'Q';
                    else if (piece is King) pieceChar = 'K';

                    // You can add further checks here if you have some way to determine the color of the piece,
                    // but since piece.Owner is not working, you might want to assume a default color.
                }
                Console.Write($"{pieceChar} ");
            }
            Console.WriteLine();
        }
    }

}

// Main Program remains the same
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
