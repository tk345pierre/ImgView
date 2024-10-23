using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Windows.Controls.Image;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;
using System.Text.RegularExpressions;

namespace ImgView
{
    public partial class MainWindow : Window
    {
        class MyImage
        {
            public string FilePath { get; set; } = "";
            public BitmapImage BitmapImg { get; set; } = null;
            public BitmapImage LoadBitmap()
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(FilePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            public void Unload()
            {
                BitmapImg = null;
            }
            public void Clear()
            {
                FilePath = "";
                BitmapImg = null;
            }
        }

        Dictionary<string, List<MyImage>> fileDictionary = null;
        private bool isRightButtonDown = false;
        private Point initialMousePosition;
        private string selectedDir = "";
        private Image lastSelectedImage = null;

        enum MainMode
        {
            ViewMode = 0,
            DelMode = 1,
        }
        private MainMode mainMode = MainMode.ViewMode;

        enum ViewMode
        {
            Max = 0,
            ImgSize = 1,
        }
        private ViewMode viewMode = ViewMode.Max;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            mainWrapPanel.Children.Clear();
            delFileList.Items.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            string imageDirectory = dirPathTextBox.Text;
            fileDictionary = GetFilesRecursively(imageDirectory);
            LoadDirBlockFromDirectory(fileDictionary);
        }

        private void LoadDirBlockFromDirectory(Dictionary<string, List<MyImage>> filesDictionary)
        {
            TextBlock selectedTextBlock = null;
            foreach (var files in filesDictionary)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

                TextBlock dirNameTextBlock = new TextBlock();
                dirNameTextBlock.Text = Regex.Replace(files.Key, Regex.Escape(dirPathTextBox.Text), string.Empty).Substring(1);
                dirNameTextBlock.FontWeight = FontWeights.Bold;
                dirNameTextBlock.Margin = new Thickness(0, 10, 0, 5);
                dirNameTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
                dirNameTextBlock.Background = new SolidColorBrush(Colors.Gray);
                dirNameTextBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
                dirNameTextBlock.MinWidth = 500;
                dirNameTextBlock.TextAlignment = TextAlignment.Center;
                dirNameTextBlock.MouseLeftButtonUp += DirNameTextBlock_MouseLeftButtonUp;
                dirNameTextBlock.Foreground = new SolidColorBrush(Colors.White);

                ItemsControl itemsControl = new ItemsControl();
                var factoryPanel = new FrameworkElementFactory(typeof(WrapPanel));
                factoryPanel.SetValue(Panel.IsItemsHostProperty, true);
                factoryPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                var template = new ItemsPanelTemplate { VisualTree = factoryPanel };
                itemsControl.ItemsPanel = template;

                stackPanel.Children.Add(dirNameTextBlock);
                stackPanel.Children.Add(itemsControl);
                mainWrapPanel.Children.Add(stackPanel);
            }
        }

        private void ClearImages()
        {
            var itemsControl = GetVisibleImagesControls();
            if (itemsControl == null) return;
            itemsControl.Items.Clear();
        }

        private bool CreateImages(TextBlock? dirTextBlock)
        {
            var dirPath = dirPathTextBox.Text + "\\" + dirTextBlock.Text;

            GetFiles(dirPath, fileDictionary);

            var myImageList = fileDictionary[dirPath];
            foreach (var myImage in myImageList)
            {
                StackPanel itemstackPanel = new StackPanel();
                itemstackPanel.Orientation = Orientation.Vertical;

                Border border = new Border();
                border.Background = new SolidColorBrush(Colors.Gray);

                var selectItem = sizeCombobox.SelectedItem as ComboBoxItem;
                var w = Int32.Parse(selectItem.Content as string);
                Image image = new Image();
                image.Width = w;
                image.MaxHeight = w * 3;
                image.MinWidth = 100;
                image.MinHeight = 100;
                image.Margin = new Thickness(5);
                image.MouseRightButtonDown += Image_MouseRightButtonDown;
                image.MouseRightButtonUp += Image_MouseRightButtonUp;
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                image.Tag = myImage;
                border.Child = image;

                TextBlock textBlock = new TextBlock();
                textBlock.Text = TruncateString(System.IO.Path.GetFileName(myImage.FilePath), 20);
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.Foreground = new SolidColorBrush(Colors.White);

                itemstackPanel.Children.Add(border);
                itemstackPanel.Children.Add(textBlock);

                StackPanel parentStackPanel = dirTextBlock.Parent as StackPanel;
                ItemsControl itemsControl = parentStackPanel.Children.OfType<ItemsControl>().FirstOrDefault();
                itemsControl.Items.Add(itemstackPanel);
            }
            return myImageList.Count > 0;
        }

        private void DirNameTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ClearImages();

            TextBlock? dirTextBlock = sender as TextBlock;
            if (selectedDir == dirTextBlock.Text)
            {
                selectedDir = "";
                return;
            }
            else
            {
                selectedDir = dirTextBlock.Text;
            }

            bool isCreated = CreateImages(dirTextBlock);
            if (isCreated)
            {
                mainScrollViewer.ScrollToVerticalOffset(dirTextBlock.TransformToAncestor(mainScrollViewer).Transform(new Point(0, 0)).Y);
            }
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 10)
            {
                List<MyImage> reducedList = fileDictionary.SelectMany(kvp => kvp.Value).ToList();
                for (int i = 0; i < reducedList.Count; i++)
                {
                    if (reducedList[i].FilePath == (mainImg.Source as BitmapImage).UriSource.LocalPath)
                    {
                        if (i == 0) break;
                        mainImg.Source = new BitmapImage(new Uri(reducedList[i - 1].FilePath, UriKind.Absolute));
                        break;
                    }
                }
            }
            else if (e.Delta < -10)
            {
                List<MyImage> reducedList = fileDictionary.SelectMany(kvp => kvp.Value).ToList();
                for (int i = 0; i < reducedList.Count; i++)
                {
                    if (reducedList[i].FilePath == (mainImg.Source as BitmapImage).UriSource.LocalPath)
                    {
                        if (reducedList.Count - 1 <= i) break;
                        mainImg.Source = new BitmapImage(new Uri(reducedList[i + 1].FilePath, UriKind.Absolute));
                        break;
                    }
                }
            }
        }

        static Dictionary<string, List<MyImage>> GetFilesRecursively(string rootDirectory)
        {
            var filesDictionary = new Dictionary<string, List<MyImage>>();
            GetDir(rootDirectory, filesDictionary);
            return filesDictionary;
        }

        static void GetFiles(string directory, Dictionary<string, List<MyImage>> filesDictionary)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*.png");
                var directories = Directory.GetDirectories(directory);

                filesDictionary[directory].Clear();
                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        filesDictionary[directory].Add(new MyImage() { FilePath = file });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {directory}: {ex.Message}");
            }
        }

        static void GetDir(string directory, Dictionary<string, List<MyImage>> filesDictionary)
        {
            try
            {
                var directories = Directory.GetDirectories(directory);

                foreach (var dir in directories)
                {
                    var list = new List<MyImage>();
                    filesDictionary[dir] = list;
                    GetDir(dir, filesDictionary);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {directory}: {ex.Message}");
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox? comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            var w = Int32.Parse(selectedItem.Content.ToString());
            var itemsControl = GetVisibleImagesControls();
            if (itemsControl == null) return;
            foreach (var spobj in itemsControl.Items)
            {
                var sp = spobj as StackPanel;
                var border = sp.Children.OfType<Border>().FirstOrDefault();
                var image = border.Child as Image;
                image.Width = w;
                image.MaxHeight = w * 3;
            }
        }

        private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRightButtonDown = true;
            initialMousePosition = e.GetPosition(null);
        }

        private void Image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickedImage = sender as Image;
            if (clickedImage == null)
            {
                return;
            }
            if (isRightButtonDown)
            {
                isRightButtonDown = false;
                Point currentMousePosition = e.GetPosition(null);
                if (currentMousePosition.Y > initialMousePosition.Y)
                {
                    setDelFileList(clickedImage);
                }
            }
        }

        private void MainImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRightButtonDown = true;
            initialMousePosition = e.GetPosition(null);
        }

        private void MainImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isRightButtonDown)
            {
                isRightButtonDown = false;
                Point currentMousePosition = e.GetPosition(null);
                if (currentMousePosition.Y > initialMousePosition.Y)
                {
                    mainImg.Stretch = viewMode == ViewMode.ImgSize ? Stretch.None : Stretch.Uniform;
                    viewMode = (ViewMode)((int)++viewMode % 2);
                }
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickedImage = sender as Image;
            if (clickedImage == null)
            {
                return;
            }
            if (mainMode == MainMode.ViewMode)
            {
                var settingFunc = (Visibility a, Visibility b, Visibility c, BitmapImage bitmap) =>
                {
                    mainStackPanel.Visibility = a;
                    mainWrapPanel.Visibility = b;
                    menuStackPanel.Visibility = c;
                    mainImg.Source = bitmap;
                };
                if (mainStackPanel.Visibility == Visibility.Collapsed)
                {
                    settingFunc(Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, new BitmapImage(new Uri(GetImageAbsolutePath(clickedImage), UriKind.Absolute)));
                }
                else
                {
                    settingFunc(Visibility.Collapsed, Visibility.Visible, Visibility.Visible, null);
                }
            }
            else
            {
                setDelFileList(clickedImage);
            }
        }

        private void setDelFileList(Image clickedImage)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (mainWrapPanel == null) return;

                bool addMode = false;
                bool reverseSelect = false;
                var itemsControl = GetVisibleImagesControls();
                if (itemsControl == null) return;
                foreach (var spobj in itemsControl.Items)
                {
                    var sp = spobj as StackPanel;
                    var border = sp.Children.OfType<Border>().FirstOrDefault();
                    var image = border.Child as Image;

                    if (addMode)
                    {
                        AddDelFileList(image);
                    }

                    if (lastSelectedImage == image)
                    {
                        addMode = !addMode;
                        if (reverseSelect)
                        {
                            AddDelFileList(image);
                        }
                    }

                    if (clickedImage == image)
                    {
                        if (!addMode)
                        {
                            reverseSelect = true;
                            AddDelFileList(image);
                        }
                        addMode = !addMode;
                    }
                }
            }
            else
            {
                AddDelFileList(clickedImage);
            }

            lastSelectedImage = clickedImage;
        }

        private void AddDelFileList(Image clickedImage)
        {
            var filePath = GetImageAbsolutePath(clickedImage);
            if (delFileList.Items.Contains(filePath))
            {
                delFileList.Items.Remove(filePath);
                (clickedImage.Parent as Border).Background = new SolidColorBrush(Colors.Gray);
                return;
            }
            delFileList.Items.Add(filePath);
            (clickedImage.Parent as Border).Background = new SolidColorBrush(Colors.Red);
        }

        private ItemsControl GetVisibleImagesControls()
        {
            ItemsControl returnVal = null;
            bool hit = false;
            if (mainWrapPanel == null) { return returnVal; }
            foreach (var wrapPanelChildren in mainWrapPanel.Children)
            {
                var stackPanel = wrapPanelChildren as StackPanel;
                foreach (var child in stackPanel.Children)
                {
                    if (child.GetType() != typeof(ItemsControl))
                    {
                        continue;
                    }
                    var itemsControl = child as ItemsControl;
                    foreach (var spobj in itemsControl.Items)
                    {
                        var sp = spobj as StackPanel;
                        var border = sp.Children.OfType<Border>().FirstOrDefault();
                        if (border != null)
                        {
                            returnVal = itemsControl;
                            hit = true;
                            break;
                        }
                    }
                    if (hit) break;
                }
                if (hit) break;
            }
            return returnVal;
        }

        private string GetImageAbsolutePath(Image image)
        {
            if (image.Source is BitmapImage bitmapImage)
            {
                return bitmapImage.UriSource.LocalPath;
            }
            return null;
        }

        public static string TruncateString(string input, int maxLength)
        {
            if (input.Length <= maxLength)
            {
                return input;
            }
            return input.Substring(0, maxLength - 3) + "...";
        }

        private void DustBoxButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pathObj in delFileList.Items)
            {
                var path = pathObj.ToString();
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            Init();

            foreach (var wrapPanelChildren in mainWrapPanel.Children)
            {
                var stackPanel = wrapPanelChildren as StackPanel;
                foreach (var child in stackPanel.Children)
                {
                    if (child.GetType() == typeof(TextBlock))
                    {
                        var textBlock = child as TextBlock;
                        if (textBlock.Text == selectedDir)
                        {
                            selectedDir = "";
                            DirNameTextBlock_MouseLeftButtonUp(textBlock, null);
                            break;
                        }
                    }
                }
            }
        }

        private void DelToggleButton_Click(object sender, RoutedEventArgs e)
        {
            mainMode = (MainMode)((int)++mainMode % 2);
            delToggleButton.Content = mainMode == MainMode.ViewMode ? "ViewMode" : "DelMode";
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var itemsControl = GetVisibleImagesControls();
            if (itemsControl == null) return;
            foreach (var spobj in itemsControl.Items)
            {
                var border = (spobj as StackPanel).Children.OfType<Border>().FirstOrDefault();
                var image = (spobj as StackPanel).Children.OfType<Border>().FirstOrDefault().Child as Image;
                if (IsInViewPort(border))
                {
                    if (image.Source == null)
                    {
                        image.Source = (image.Tag as MyImage).LoadBitmap();
                    }
                }
                else
                {
                    (image.Tag as MyImage).Unload();
                }
            }
        }

        private bool IsInViewPort(UIElement element)
        {
            GeneralTransform transform = element.TransformToAncestor(mainScrollViewer);
            Rect elementBounds = transform.TransformBounds(new Rect(new Point(0, 0), element.RenderSize));
            Rect viewportBounds = new Rect(new Point(0, 0), mainScrollViewer.RenderSize);
            return viewportBounds.IntersectsWith(elementBounds);
        }

        private void dirPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Init();
            }
        }
    }
}