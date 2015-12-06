#define USE_CANVAS

using System;
using System.Collections.Generic;
using System.ComponentModel;        // For BackgroundWorker
using System.IO;
using System.Linq;
using System.Net;                   // For HTTP stuff
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using WPFTicTacToe.GameEngine;
//using GameEngine = WPFTicTacToe.GameEngine.GameEngine;    // This does not pull the GameEngine class into the top-level namespace.

namespace WPFTicTacToe
{
    public enum GameEngineServiceType
    {
        InProcess,
        CSharpSOAP,
        JavaSOAP,
        PHPREST, // Ubuntu VM
        PHPRESTLocalhost,
        PHPRESTXPVM,
        PerlRESTUbuntuVM
    }

    // See http://social.msdn.microsoft.com/Forums/en/wpf/thread/97656b9b-5d26-4e86-bbd5-d0a53e9c5ec0

    public static class WPFObjectCopier
    {
        public static T Clone<T>(T source)
        {
            string objXaml = XamlWriter.Save(source);
            StringReader stringReader = new StringReader(objXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            T t = (T)XamlReader.Load(xmlReader);

            return t;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IGameWindow
    {
        private int boardDimension = 3;
        private GameEngine.GameEngine gameEngine;
#if USE_CANVAS
        private readonly Dictionary<string, Canvas> canvasDictionary = new Dictionary<string, Canvas>();
        private readonly Canvas canvasX;
        private readonly Canvas canvasO;
#else
        private readonly Dictionary<string, Image> imageDictionary = new Dictionary<string, Image>();
        private readonly BitmapImage bitmapEmpty;
        private readonly BitmapImage bitmapX;
        private readonly BitmapImage bitmapO;
#endif
        private readonly Dictionary<int, MenuItem> resizeBoardMenuItemDictionary = new Dictionary<int, MenuItem>();
        private GameEngineServiceType gameEngineServiceType = GameEngineServiceType.InProcess;
        private readonly Dictionary<GameEngineServiceType, MenuItem> gameEngineServiceTypeMenuItemDictionary = new Dictionary<GameEngineServiceType, MenuItem>();
        private readonly BackgroundWorker worker;
        private bool beginNewGame = false;  // This is used to begin a new game after the BackgroundWorker has been cancelled.

        public MainWindow()
        {
            InitializeComponent();

            worker = (BackgroundWorker)FindResource("backgroundWorker");

            SetUpImageGrid();

            gameEngine = new GameEngine.GameEngine(this, boardDimension, null, worker);

#if USE_CANVAS
            canvasX = (Canvas)FindResource("XCanvas");
            canvasO = (Canvas)FindResource("OCanvas");
#else
            bitmapEmpty = new BitmapImage(new Uri("/Images/Empty.png", UriKind.Relative));
            bitmapX = new BitmapImage(new Uri("/Images/X.png", UriKind.Relative));
            bitmapO = new BitmapImage(new Uri("/Images/O.png", UriKind.Relative));
#endif

            automateX.IsChecked = gameEngine.isAutomated[SquareContentType.X];
            automateO.IsChecked = gameEngine.isAutomated[SquareContentType.O];

            tbXPly.Text = this.gameEngine.playerPlyDictionary[SquareContentType.X].ToString();
            tbOPly.Text = this.gameEngine.playerPlyDictionary[SquareContentType.O].ToString();

            resizeBoardMenuItemDictionary[3] = menuItem_Resize3x3;
            resizeBoardMenuItemDictionary[4] = menuItem_Resize4x4;

            SetResizeBoardMenuItemCheckboxes();

            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.InProcess] = menuItem_gameEngine_InProcess;
            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.CSharpSOAP] = menuItem_gameEngine_CSharpSOAP;
            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.JavaSOAP] = menuItem_gameEngine_JavaSOAP;
            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.PHPREST] = menuItem_gameEngine_PHPREST;
            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.PHPRESTLocalhost] = menuItem_gameEngine_PHPRESTLocalhost;
            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.PHPRESTXPVM] = menuItem_gameEngine_PHPRESTXPVM;
            gameEngineServiceTypeMenuItemDictionary[GameEngineServiceType.PerlRESTUbuntuVM] = menuItem_gameEngine_PerlRESTUbuntuVM;

            SetGameEngineServiceTypeMenuItemCheckboxes();

            BeginNewGame();
        }

#if USE_CANVAS
        private void CopyCanvasContents(Canvas source, Canvas destination)
        {
            destination.Children.Clear();

            foreach (UIElement child in source.Children)
            {
                destination.Children.Add(WPFObjectCopier.Clone<UIElement>(child));
            }
        }
#endif

        private void SetResizeBoardMenuItemCheckboxes()
        {

            foreach (KeyValuePair<int, MenuItem> kvp in resizeBoardMenuItemDictionary)
            {
                kvp.Value.IsChecked = (kvp.Key == boardDimension);
            }
        }

        private void SetGameEngineServiceTypeMenuItemCheckboxes()
        {

            foreach (var kvp in gameEngineServiceTypeMenuItemDictionary)
            {
                kvp.Value.IsChecked = (kvp.Key == gameEngineServiceType);
            }
        }

        public void SetUpImageGrid()
        {
            boardGrid.Children.Clear();
            boardGrid.RowDefinitions.Clear();
            boardGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < boardDimension; ++i)
            {
                var rowDefinition = new RowDefinition();
                var columnDefinition = new ColumnDefinition();

                rowDefinition.Height = new GridLength(1.0, GridUnitType.Star);
                columnDefinition.Width = new GridLength(1.0, GridUnitType.Star);
                boardGrid.RowDefinitions.Add(rowDefinition);
                boardGrid.ColumnDefinitions.Add(columnDefinition);
            }

#if USE_CANVAS
            canvasDictionary.Clear();
#else
            imageDictionary.Clear();
#endif

            int index = 0;

            for (int row = 0; row < boardDimension; ++row)
            {

                for (int column = 0; column < boardDimension; ++column)
                {
                    var indexAsString = index.ToString();
#if USE_CANVAS
                    var canvas = new Canvas();

                    Grid.SetRow(canvas, row);
                    Grid.SetColumn(canvas, column);
                    canvas.Name = "square" + indexAsString;
                    canvas.Tag = indexAsString;
                    boardGrid.Children.Add(canvas);
                    canvasDictionary[indexAsString] = canvas;
#else
                    var image = new Image();

                    Grid.SetRow(image, row);
                    Grid.SetColumn(image, column);
                    image.Name = "square" + indexAsString;
                    image.Tag = indexAsString;
                    boardGrid.Children.Add(image);
                    imageDictionary[indexAsString] = image;
#endif
                    ++index;
                }
            }
        }

        public void PlacePiece(SquareContentType piece, int row, int column)
        {
            var squareIndex = row * boardDimension + column;
            var squareIndexAsString = squareIndex.ToString();

#if USE_CANVAS
            if (!canvasDictionary.ContainsKey(squareIndexAsString))
            {
                DisplayMessage(string.Format("PlacePiece() : No square with tag '{0}'", squareIndexAsString));
                return;
            }

            var sourceCanvas = (piece == SquareContentType.X) ? canvasX : canvasO;

            CopyCanvasContents(sourceCanvas, canvasDictionary[squareIndexAsString]);
#else
            if (!imageDictionary.ContainsKey(squareIndexAsString))
            {
                DisplayMessage(string.Format("PlacePiece() : No square with tag '{0}'", squareIndexAsString));
                return;
            }

            var squareImage = imageDictionary[squareIndexAsString];

            squareImage.Source = (piece == SquareContentType.X) ? bitmapX : bitmapO;
#endif
        }

        public void DisplayMessage(string message)
        {
            messageLabel.Text = message;
        }

        private void BeginNewGame()
        {

            if (this.worker.CancellationPending)    // In case the user clicks the "New Game" button twice in a row, very rapidly.
            {
                return;
            }

            if (this.worker.IsBusy && !this.beginNewGame) // !beginNewGame prevents us from trying to cancel the BackgroundWorker immediately again.
            {
                this.worker.CancelAsync();
                return; // BackgroundWorker_RunWorkerCompleted() will call us back when the cancellation is complete.
            }

            this.beginNewGame = false;

#if USE_CANVAS
            foreach (var canvas in canvasDictionary.Values)
            {
                canvas.Children.Clear();
            }
#else
            foreach (var image in imageDictionary.Values)
            {
                image.Source = bitmapEmpty;
            }
#endif

            gameEngine.BeginNewGame();
            DisplayTurnMessage();

            if (this.gameEngine.isAutomated[this.gameEngine.currentPlayer])
            {
                AutomatedMove();
            }
        }

        private void DisplayTurnMessage()
        {
            DisplayMessage(string.Format("{0}'s turn.", this.gameEngine.playerNameDictionary[this.gameEngine.currentPlayer]));
        }

        private void DisplayGameOverMessage()
        {

            if (this.gameEngine.isGameOver)
            {
                DisplayMessage(string.Format("{0} wins!", this.gameEngine.playerNameDictionary[this.gameEngine.currentPlayer]));
            }
            else if (this.gameEngine.boardPopulation == this.gameEngine.boardArea)
            {
                DisplayMessage("Tie game.");
            }
        }

        private void MoveHelper(int row, int column)
        {
            this.gameEngine.PlacePiece(this.gameEngine.currentPlayer, row, column, true);

            if (this.gameEngine.isGameOver || this.gameEngine.boardPopulation == this.gameEngine.boardArea)
            {
                DisplayGameOverMessage();
                this.Cursor = Cursors.Arrow;
                return;
            }

            this.gameEngine.currentPlayer = (this.gameEngine.currentPlayer == SquareContentType.X) ? SquareContentType.O : SquareContentType.X;
            DisplayTurnMessage();

            if (!this.gameEngine.isAutomated[this.gameEngine.currentPlayer])
            {
                this.Cursor = Cursors.Arrow;
                return;
            }

            AutomatedMove();
        }

        private void AutomatedMove()
        {
            this.Cursor = Cursors.Wait;
            worker.RunWorkerAsync();
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (this.gameEngine.isGameOver || this.gameEngine.isAutomated[this.gameEngine.currentPlayer])
            {
                return;
            }

            // Bust that funky disco move, white boy.
#if USE_CANVAS
            Canvas canvasSender = sender as Canvas;

            if (canvasSender == null)
            {
                DisplayMessage("Image_MouseUp() : sender is not a Canvas");
                return;
            }

            int squareIndex;

            if (!int.TryParse(canvasSender.Tag.ToString(), out squareIndex))
            {
                DisplayMessage(string.Format("Image_MouseUp() : canvasSender.Tag '{0}' is not an int", canvasSender.Tag));
                return;
            }
#else
            Image imgSender = sender as Image;

            if (imgSender == null)
            {
                DisplayMessage("Image_MouseUp() : sender is not an Image");
                return;
            }

            int squareIndex;

            if (!int.TryParse(imgSender.Tag.ToString(), out squareIndex))
            {
                DisplayMessage(string.Format("Image_MouseUp() : imgSender.Tag '{0}' is not an int", imgSender.Tag));
                return;
            }
#endif

            if (this.gameEngine.board[squareIndex] != SquareContentType.EmptySquare)
            {
                DisplayMessage(string.Format("Error: Square not empty.  {0}'s turn.", this.gameEngine.playerNameDictionary[this.gameEngine.currentPlayer]));
                return;
            }

            int row = squareIndex / boardDimension;
            int column = squareIndex % boardDimension;

            MoveHelper(row, column);
        }

        private void automateX_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;

            if (checkbox == null)
            {
                DisplayMessage("sender is not a CheckBox");
                return;
            }

            this.gameEngine.isAutomated[SquareContentType.X] = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
        }

        private void automateO_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;

            if (checkbox == null)
            {
                DisplayMessage("sender is not a CheckBox");
                return;
            }

            this.gameEngine.isAutomated[SquareContentType.O] = checkbox.IsChecked.HasValue && checkbox.IsChecked.Value;
        }

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            BeginNewGame();
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void tbXPly_LostFocus(object sender, RoutedEventArgs e)
        {
            int ply;

            if (int.TryParse(tbXPly.Text, out ply) &&
                ply >= GameEngine.GameEngine.minimumPly &&
                ply <= GameEngine.GameEngine.maximumPly &&
                ply <= this.gameEngine.boardArea)
            {
                this.gameEngine.playerPlyDictionary[SquareContentType.X] = ply;
            }
            else
            {
                tbXPly.Text = this.gameEngine.playerPlyDictionary[SquareContentType.X].ToString();
            }
        }

        private void tbOPly_LostFocus(object sender, RoutedEventArgs e)
        {
            int ply;

            if (int.TryParse(tbOPly.Text, out ply) &&
                ply >= GameEngine.GameEngine.minimumPly &&
                ply <= GameEngine.GameEngine.maximumPly &&
                ply <= this.gameEngine.boardArea)
            {
                this.gameEngine.playerPlyDictionary[SquareContentType.O] = ply;
            }
            else
            {
                tbOPly.Text = this.gameEngine.playerPlyDictionary[SquareContentType.O].ToString();
            }
        }

        private void OnResizeBoard(int newDimension)
        {

            if (newDimension != boardDimension)
            {
                boardDimension = newDimension;

                SetUpImageGrid();

                gameEngine = new GameEngine.GameEngine(this, boardDimension);

                BeginNewGame();
            }

            SetResizeBoardMenuItemCheckboxes();
        }

        private void Resize3_Click(object sender, RoutedEventArgs e)
        {
            OnResizeBoard(3);
        }

        private void Resize4_Click(object sender, RoutedEventArgs e)
        {
            OnResizeBoard(4);
        }

        private void OnChangeGameEngineService(GameEngineServiceType g)
        {
            gameEngineServiceType = g;
            SetGameEngineServiceTypeMenuItemCheckboxes();
        }

        private void SelectGameEngine_InProcess_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.InProcess);
        }

        private void SelectGameEngine_CSharpSOAP_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.CSharpSOAP);
        }

        private void SelectGameEngine_JavaSOAP_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.JavaSOAP);
        }

        private void SelectGameEngine_PHPREST_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.PHPREST);
        }

        private void SelectGameEngine_PHPRESTLocalhost_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.PHPRESTLocalhost);
        }

        private void SelectGameEngine_PHPRESTXPVM_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.PHPRESTXPVM);
        }

        private void SelectGameEngine_PerlRESTUbuntuVM_Click(object sender, RoutedEventArgs e)
        {
            OnChangeGameEngineService(GameEngineServiceType.PerlRESTUbuntuVM);
        }

        private int GetHTTPResponseAsInt(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {

                if (ex.Response == null)
                {
                    throw;
                }

                response = (HttpWebResponse)ex.Response;
            }

            Stream objStream = response.GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);
            var sLine = objReader.ReadLine();

            if (string.IsNullOrEmpty(sLine))
            {
                throw new Exception("The HTTP response has an empty body");
            }

            var statusCode = (int)response.StatusCode;

            if (statusCode < 200 || statusCode >= 300)
            {
                throw new Exception(sLine);
            }

            int result;

            if (!int.TryParse(sLine, out result))
            {
                throw new Exception(string.Format("'{0}' is not an integer", sLine));
            }

            return result;
        }

        private BestMoveData CallRESTfulService(string server)
        {
            var bestSquareIndex = GetHTTPResponseAsInt(string.Format(
                //"http://{0}/service.php?board={1}&ply={2}",
                "http://{0}/{1}/{2}", // RESTful
                server,
                this.gameEngine.GetBoardAsString().Replace(' ', 'E'),
                this.gameEngine.playerPlyDictionary[this.gameEngine.currentPlayer]));
            var bestRow = bestSquareIndex / this.gameEngine.boardDimension;
            var bestColumn = bestSquareIndex % this.gameEngine.boardDimension;

            return new BestMoveData(0, bestRow, bestColumn);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {

                switch (gameEngineServiceType)
                {
                    case GameEngineServiceType.InProcess:
                        e.Result = this.gameEngine.FindBestMoveWrapper(this.gameEngine.currentPlayer,
                            this.gameEngine.playerPlyDictionary[this.gameEngine.currentPlayer]);
                        e.Cancel = this.worker.CancellationPending; // Is this the right way to do this?
                        break;

                    case GameEngineServiceType.CSharpSOAP:
                        {
                            var gameEngineServiceClient = new GameEngineServiceClient();
                            int bestSquareIndex = gameEngineServiceClient.FindBestMove(
                                this.gameEngine.boardDimension,
                                this.gameEngine.GetBoardAsString(),
                                this.gameEngine.currentPlayer == SquareContentType.X,
                                this.gameEngine.playerPlyDictionary[this.gameEngine.currentPlayer]);
                            int bestRow = bestSquareIndex / this.gameEngine.boardDimension;
                            int bestColumn = bestSquareIndex % this.gameEngine.boardDimension;

                            e.Result = new BestMoveData(0, bestRow, bestColumn);
                            // TODO: Support cancellation when the game engine service is being used, if possible.
                        }

                        break;

                    case GameEngineServiceType.JavaSOAP:
                        {
                            var gameEngineServiceClient = new ServiceReference2.SOAPTicTacToeClient();
                            int bestSquareIndex = gameEngineServiceClient.findBestMove(
                                this.gameEngine.boardDimension,
                                this.gameEngine.GetBoardAsString(),
                                this.gameEngine.currentPlayer == SquareContentType.X ? "X" : "O",
                                this.gameEngine.playerPlyDictionary[this.gameEngine.currentPlayer]);
                            int bestRow = bestSquareIndex / this.gameEngine.boardDimension;
                            int bestColumn = bestSquareIndex % this.gameEngine.boardDimension;

                            e.Result = new BestMoveData(0, bestRow, bestColumn);
                        }

                        break;

                    case GameEngineServiceType.PHPREST:
                        e.Result = CallRESTfulService("192.168.56.11/php/tictactoe");
                        break;

                    case GameEngineServiceType.PHPRESTLocalhost:
                        e.Result = CallRESTfulService("localhost:8080/php/tictactoe");
                        break;

                    case GameEngineServiceType.PHPRESTXPVM:
                        e.Result = CallRESTfulService("192.168.56.13:8080/php/tictactoe");
                        break;

                    case GameEngineServiceType.PerlRESTUbuntuVM:
                        e.Result = CallRESTfulService("192.168.56.11/perl/tictactoe");
                        break;

                    default:
                        throw new Exception("BackgroundWorker_DoWork: Unknown gameEngineServiceType");
                }
                /*
                for (int i = 1; i <= 100; i++)
                {
                    if (worker.CancellationPending)
                        break;

                    Thread.Sleep(100);
                    worker.ReportProgress(i);
                }
                 */
            }
            catch (Exception ex)
            {
                e.Result = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (e.Cancelled)
            {
                beginNewGame = true;
                BeginNewGame();
                return;
            }

            if (e.Result is string)
            {
                DisplayMessage((string)e.Result); // If the worker threw an exception, display the corresponding message.

                gameEngine.isAutomated[SquareContentType.X] = false;
                gameEngine.isAutomated[SquareContentType.O] = false;
                automateX.IsChecked = false;
                automateO.IsChecked = false;

                this.Cursor = Cursors.Arrow;
            }
            else
            {
                var bestMoveData = e.Result as BestMoveData;

                MoveHelper(bestMoveData.row, bestMoveData.column);
                /*
                this.Cursor = Cursors.Arrow;
                Console.WriteLine(e.Error.Message);
                button.Content = "Start";
                 */
            }
        }

        /*
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
         */
    }
}
