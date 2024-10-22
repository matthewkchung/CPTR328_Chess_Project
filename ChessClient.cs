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

                        string[] moveParts = moveInput.Split(' ');
                        if (moveParts.Length != 2)
                        {
                            Console.WriteLine("Invalid input. Format: 'from to'");
                            continue;
                        }

                        // Process the move
                        if (TryMakeMove(moveParts[0], moveParts[1], Player.Black))
                        {
                            PrintBoard();

                            // Send the move to the server (White)
                            ChessMove move = new ChessMove { From = moveParts[0], To = moveParts[1] };
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
                }
                Console.Write($"{pieceChar} ");
            }
            Console.WriteLine();
        }
    }

}
