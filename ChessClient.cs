using ChessDotNet.Pieces;
using ChessDotNet;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

public class ChessClient
{
    private TcpClient client;
    private ChessGame game;

    public ChessClient()
    {
        game = new ChessGame();
    }

    private void PrintBoard()
    {
        var board = game.GetBoard();
        ChessUtilities.PrintBoard(board);
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
                Player currentTurn = Player.Black; // The client is Black

                while (true)
                {
                    if (game.WhoseTurn == Player.White)
                    {
                        // Wait for the server's (White) move
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // Connection closed

                        string moveData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ChessMove receivedMove = JsonConvert.DeserializeObject<ChessMove>(moveData);

                        // Apply the move from White
                        if (TryMakeMove(receivedMove.From, receivedMove.To, Player.White))
                        {
                            Console.WriteLine($"Opponent moved: {receivedMove.From} to {receivedMove.To}");
                            PrintBoard();

                            // Now it's Black's turn
                            currentTurn = Player.Black;
                        }
                        else
                        {
                            Console.WriteLine("Invalid move received from opponent!");
                        }
                    }
                    else
                    {
                        // Black's turn to move
                        Console.WriteLine("Your move (Black): ");
                        string moveInput = Console.ReadLine();
                        string from = "";
                        string to = "";
                        bool isCastling = false;

                        // First, check if it is castles short
                        if (moveInput == "O-O")
                        {
                            isCastling = true;
                            Console.WriteLine("Castling short...");
                            from = "e8";
                            to = "g8";
                        }

                        else if (moveInput == "O-O-O")
                        {
                            isCastling = true;
                            Console.WriteLine("Castling long...");
                            from = "e8";
                            to = "c8";
                        }

                        string[] moveParts = moveInput.Split(' ');
                        if (moveParts.Length != 2 && !isCastling)
                        {
                            Console.WriteLine("Invalid input. Format: 'from to'");
                            continue;
                        }

                        // If it is a pawn move
                        if (moveParts[1].Length == 2)
                        {
                            from = moveParts[0];
                            to = moveParts[1];
                        }

                        else if (moveParts[1].Length != 2 && !isCastling) // It is moving a "piece"
                        {
                            // Split the move into the piece + the position
                            char movingPiece = moveParts[0][0];
                            from = moveParts[0].Substring(1); // Get the rest for og position
                            to = moveParts[1].Substring(1); // Get the position for dest position

                            Console.WriteLine($"Moving Piece Type: {movingPiece}, Original Position: {from}, New Position: {to}");
                        }

                        // Process the move
                        if (TryMakeMove(from, to, Player.Black))
                        {
                            PrintBoard();

                            // Send the move to the server (White)
                            ChessMove move = new ChessMove { From = from, To = to };
                            string moveData = JsonConvert.SerializeObject(move);
                            byte[] moveBytes = Encoding.UTF8.GetBytes(moveData);
                            await stream.WriteAsync(moveBytes, 0, moveBytes.Length);

                            // Switch back to White's turn
                            currentTurn = Player.White;
                        }
                        else
                        {
                            Console.WriteLine("Invalid move!");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }


}
