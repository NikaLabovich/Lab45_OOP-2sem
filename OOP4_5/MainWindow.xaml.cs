using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.IO;
using System.Windows.Resources;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace OOP4_5
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
      

        private void ThemeChange(object sender, SelectionChangedEventArgs e)
        {
            string style = styleBox.SelectedItem as string;
            // определяем путь к файлу ресурсов
            var uri = new Uri(style + ".xaml", UriKind.Relative);
            // загружаем словарь ресурсов
            ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
            // очищаем коллекцию ресурсов приложения
            Application.Current.Resources.Clear();
            // добавляем загруженный словарь ресурсов
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }
        bool WasUndoOrRedu { get; set; }
        string OldText { get; set; }
        Stack<string> UndoStack { get; set; } = new Stack<string>();
        Stack<string> RedoStack { get; set; } = new Stack<string>();

     
       List<string> RecentFiles  = new List<string>();



        string GetRtfString()
        {
            TextRange textRange = new TextRange(EditableText.Document.ContentStart, EditableText.Document.ContentEnd);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                textRange.Save(memoryStream, DataFormats.Rtf);

                return Encoding.Default.GetString(memoryStream.ToArray());
            }
        }

        void SetRtfString(string s)
        {
            FlowDocument flowDocument = new FlowDocument();
            using (MemoryStream memoryStream = new MemoryStream(Encoding.Default.GetBytes(s)))
            {
                TextRange textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                textRange.Load(memoryStream, DataFormats.Rtf);
                EditableText.Document = flowDocument;
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            //Set cursor
            StreamResourceInfo sri = Application.GetResourceStream(new Uri("Resourses/Pen.cur", UriKind.Relative));
            Cursor customCursor = new Cursor(sri.Stream);
            WindowApp.Cursor = customCursor;
            EditableText.Cursor = customCursor;

            FontSizeSlider.Value = 12;
            EditableText.AllowDrop = true;
            FontTypes.ItemsSource = Fonts.SystemFontFamilies.OrderBy(x => x.Source);

            this.EditableText.AddHandler(RichTextBox.DragOverEvent, new DragEventHandler(this.EditableText_DragOver), true);
            this.EditableText.AddHandler(RichTextBox.DropEvent, new DragEventHandler(this.EditableText_Drop), true);

            List<string> styles = new List<string> { "Light", "Dark" };
            styleBox.SelectionChanged += ThemeChange;
            styleBox.ItemsSource = styles;
            styleBox.SelectedItem = "Light";
        }
        //Open file
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "RichText Files (*.rtf)|*.rtf|All files (*.*)|*.*";

            if (ofd.ShowDialog() == true)
            {

                OpenFile(ofd.FileName);
                RecentFiles.Insert(0, ofd.FileName);
                if (RecentFiles.Count > 10)
                {
                    RecentFiles.RemoveAt(RecentFiles.Count-1);
                }

                UpdateRecentFiles();

            }  
        }

        private void OpenFile(string filename)
        {
            TextRange doc = new TextRange(EditableText.Document.ContentStart, EditableText.Document.ContentEnd);
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                if (Path.GetExtension(filename).ToLower() == ".rtf")
                    doc.Load(fs, DataFormats.Rtf);
                else if (Path.GetExtension(filename).ToLower() == ".txt")
                    doc.Load(fs, DataFormats.Text);
                else
                    doc.Load(fs, DataFormats.Xaml);
            }
        }

        private void UpdateRecentFiles()
        {
            MenuItemRecentFiles.Items.Clear();
            for (int i = 0; i < RecentFiles.Count; i++)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = $"{i+1} {RecentFiles[i]}";
                string filename = RecentFiles[i];
                menuItem.Click += (sender, e)=> OpenFile(filename);
                MenuItemRecentFiles.Items.Add(menuItem);

            }
        }


        //Save file
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter =
                "Файл XAML (*.xaml)|*.xaml|RTF-файл (*.rtf)|*.rtf|TXT-файл (*.txt)|*.txt";

            if (save.ShowDialog() == true)
            {
                // Создание контейнера TextRange для всего документа
                TextRange documentTextRange = new TextRange(
                    EditableText.Document.ContentStart, EditableText.Document.ContentEnd);

                // Если такой файл существует, он перезаписывается, 
                using (FileStream fs = File.Create(save.FileName))
                {
                    if (System.IO.Path.GetExtension(save.FileName).ToLower() == ".rtf")
                    {
                        documentTextRange.Save(fs, DataFormats.Rtf);
                    }
                    else if (System.IO.Path.GetExtension(save.FileName).ToLower() == ".txt")
                    {
                        documentTextRange.Save(fs, DataFormats.Text);
                    }
                    else
                    {
                        documentTextRange.Save(fs, DataFormats.Xaml);
                    }
                }
            }
        }

        //Clear file
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            EditableText.Document.Blocks.Clear();
        }

        //Text-size slider
        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                EditableText.Selection.ApplyPropertyValue(FontSizeProperty, e.NewValue);

            }
        }



        //Font-family updating
        private void EditableText_FontUpdate(object sender, RoutedEventArgs e)
        {
            object temp = this.EditableText.Selection.GetPropertyValue(Inline.FontFamilyProperty);
            this.FontTypes.SelectedItem = temp;
        }


        private void Font_Select(object sender, RoutedEventArgs e)
        {
            if (FontTypes.SelectedItem != null && EditableText != null)
            {
                EditableText.Selection.ApplyPropertyValue(System.Windows.Controls.RichTextBox.FontFamilyProperty, FontTypes.SelectedItem);
                EditableText.Focus();
            }
        }

        //BUI buttons
        private void Bold_Unchecked(object sender, RoutedEventArgs e)
        {
            this.EditableText.FontWeight = FontWeights.Normal;
        }

        private void Italic_Unchecked(object sender, RoutedEventArgs e)
        {
            this.EditableText.FontStyle = FontStyles.Normal;
        }

        private void UnderLine_Unchecked(object sender, RoutedEventArgs e)
        {
            this.EditableText.FontStyle = FontStyles.Normal;
        }

        //Footer label
        private void EditableText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WasUndoOrRedu)
            {
                WasUndoOrRedu = false;
            }
            else
            {
                UndoStack.Push(OldText);
                OldText = GetRtfString();
            }
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = UndoStack.Count != 0;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WasUndoOrRedu = true;
            RedoStack.Push(GetRtfString());
            string s = UndoStack.Pop();
            SetRtfString(s);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OldText = GetRtfString();
        }

        private void Redu_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = RedoStack.Count != 0;
        }

        private void Redu_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WasUndoOrRedu = true;
            UndoStack.Push(GetRtfString());
            string s = RedoStack.Pop();
            SetRtfString(s);
        }

        //Localization
        private void RussianLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Resources = new ResourceDictionary()
                {
                    Source = new Uri("pack://application:,,,/Resourses/langRU.xaml")
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("error: " + ex.Message);
            }
        }
        private void EnglishLanguage_Click(object sender, RoutedEventArgs e)
        {
            {
                try
                {
                    this.Resources = new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/Resourses/langEN.xaml")
                    };
                }
                catch (Exception ex)
                {
                    MessageBox.Show("error: " + ex.Message);
                }
            }
        }

        private void FranchLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Resources = new ResourceDictionary()
                {
                    Source = new Uri("pack://application:,,,/Resourses/langFR.xaml")
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("error: " + ex.Message);
            }
        }
        //Color
        //private void ColorButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var clickedButton = sender as Button;
        //    var clickedColor = clickedButton.Background;
        //    Header.Background = clickedColor;
        //    StatusBar.Background = clickedColor;
        //    Menu.Background = clickedColor;
        //}

        //DnD
        private void EditableText_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        private void EditableText_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] docPath = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (System.IO.File.Exists(docPath[0]))
                {
                    try
                    {
                        TextRange range = new TextRange(this.EditableText.Document.ContentStart, this.EditableText.Document.ContentEnd);
                        FileStream fStream = new FileStream(docPath[0], FileMode.OpenOrCreate);
                        range.Load(fStream, DataFormats.Rtf);
                        fStream.Close();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("File could not be opened. Make sure the file is a text file.");
                    }
                }
            }
        }

        private void ClearButton_Click(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void WindowApp_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecentFiles.dat"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, RecentFiles);
            }
        }

        private void WindowApp_Loaded(object sender, RoutedEventArgs e)
        {
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecentFiles.dat"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                RecentFiles = (List<string>)formatter.Deserialize(stream);
                UpdateRecentFiles();
            }
        }

        private void OpenButton_Click(object sender, ExecutedRoutedEventArgs e)
        {

        }
    }
}
