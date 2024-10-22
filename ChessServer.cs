using ChessDotNet.Pieces;
using ChessDotNet;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net;
using System.Text;

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
            // Check if it's the right player's turn
            if (game.WhoseTurn != player)
            {
                Console.WriteLine("It's not your turn!");
                return false;
            }

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

            Player currentTurn = Player.White; // White starts the game

            while (true)
            {
                try
                {
                    // Only allow the host (White) to make the first move
                    if (game.WhoseTurn == Player.White)
                    {
                        Console.WriteLine("Your move (White): ");
                        string moveInput = Console.ReadLine();

                        string[] moveParts = moveInput.Split(' ');
                        if (moveParts.Length != 2)
                        {
                            Console.WriteLine("Invalid input. Format: 'from to'");
                            continue;
                        }

                        string from;
                        string to;

                        // If it is a pawn move
                        if (moveParts[0].StartsWith("a") || moveParts[0].StartsWith("b") ||
                            moveParts[0].StartsWith("c") || moveParts[0].StartsWith("d") ||
                            moveParts[0].StartsWith("e") || moveParts[0].StartsWith("f") ||
                            moveParts[0].StartsWith("g") || moveParts[0].StartsWith("h")
                            )
                        {
                            from = moveParts[0];
                            to = moveParts[1];
                        }

                        else // If it is moving a "piece"
                        {
                            // Split the move into the piece + the position
                            char movingPiece = moveParts[0][0];
                            from = moveParts[0].Substring(1); // Get the rest for og position
                            to = moveParts[1].Substring(1); // Get the position for dest position

                            Console.WriteLine($"Moving Piece Type: {movingPiece}, Original Position: {from}, New Position: {to}");
                        }

                        // Process the move
                        if (TryMakeMove(from, to, Player.White))
                        {
                            PrintBoard();

                            // Send the move to the client (Black)
                            ChessMove move = new ChessMove { From = from, To = to };
                            string moveData = JsonConvert.SerializeObject(move);
                            byte[] moveBytes = Encoding.UTF8.GetBytes(moveData);
                            await stream.WriteAsync(moveBytes, 0, moveBytes.Length);

                            // Switch to Black's turn
                            currentTurn = Player.Black;
                        }
                        else
                        {
                            Console.WriteLine("Invalid move! " + moveParts[0] + " " + moveParts[1] + " is " +
                                "not a legal move in this position!");
                        }
                    }
                    else
                    {
                        // Receive move from the client (Black)
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // Connection closed

                        string moveData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ChessMove receivedMove = JsonConvert.DeserializeObject<ChessMove>(moveData);

                        // Apply the client's move (Black)
                        if (TryMakeMove(receivedMove.From, receivedMove.To, Player.Black))
                        {
                            Console.WriteLine($"Opponent moved: {receivedMove.From} to {receivedMove.To}");
                            PrintBoard();

                            // Switch back to White's turn
                            currentTurn = Player.White;
                        }
                        else
                        {
                            Console.WriteLine("Invalid move received from opponent!");
                        }
                    }
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
