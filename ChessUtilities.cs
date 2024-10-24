// ChessUtilities.cs
using ChessDotNet.Pieces;
using ChessDotNet;
public static class ChessUtilities
{
    public static void PrintBoard(Piece[][] board)
    {
        Console.Clear(); // Clear the console before printing the board
        Console.WriteLine("   A B C D E F G H");
        Console.WriteLine("   ---------------");

        // Print the board with ranks 1 to 8 from White's perspective
        for (int rank = 0; rank < 8; rank++)
        {
            Console.Write($"{8 - rank}| "); // Display rank in descending order
            for (int file = 0; file < 8; file++)
            {
                var piece = board[rank][file]; // Get the piece at the current file and rank
                char pieceChar = '.'; // Default for empty square

                if (piece != null)
                {
                    // Get the appropriate character based on piece type
                    if (piece is Pawn) pieceChar = 'P';
                    else if (piece is Rook) pieceChar = 'R';
                    else if (piece is Knight) pieceChar = 'N';
                    else if (piece is Bishop) pieceChar = 'B';
                    else if (piece is Queen) pieceChar = 'Q';
                    else if (piece is King) pieceChar = 'K';

                    // Indicate piece color, uppercase for white, lowercase for black
                    pieceChar = piece.Owner == Player.White ? char.ToUpper(pieceChar) : char.ToLower(pieceChar);
                }
                Console.Write($"{pieceChar} "); // Print the piece character with a space
            }
            Console.WriteLine(); // Move to the next line after printing all files for the current rank
        }
        Console.WriteLine(); // Add an extra line for better readability
    }

    public static bool IsGameOver(ChessGame game)
    {
        Player currentPlayer = game.WhoseTurn;

        // Check if the current player is checkmated
        if (game.IsCheckmated(currentPlayer))
        {
            Console.WriteLine($"{currentPlayer} is checkmated! Game over.");
            return true;
        }
        else if (game.IsStalemated(currentPlayer))
        {
            Console.WriteLine($"{currentPlayer} is stalemated! Game over.");
            return true;
        }
        return false;
    }


}
