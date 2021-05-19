using CleanUpPlanningSuiteCode;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Reflection;
using ICSharpCode.AvalonEdit.Folding;
using ControlzEx.Theming;

namespace CleanUpPlanningSuiteCode2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();


            Assembly assy = Assembly.GetExecutingAssembly();
            string fileName ="DMIS.xshd";
            string resourcePath = string.Empty;

            
            resourcePath = assy.GetManifestResourceNames().Single(str => str.EndsWith(fileName));

            using (Stream stream = assy.GetManifestResourceStream(resourcePath))
            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                IHighlightingDefinition hld = HighlightingLoader.Load(reader,HighlightingManager.Instance);
                txbInputFile.SyntaxHighlighting = hld;
                txbOutput.SyntaxHighlighting = hld;
            }
        }

        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select a file to open",
                Filter = "DMIS Files (*.dmi|*.dmi"
            };

            if ((bool)ofd.ShowDialog())
            {
                using (StreamReader sr = new StreamReader(ofd.FileName))
                {
                    txbInputFile.Text = sr.ReadToEnd();
                    sr.Close();
                }
                CleanUpCode();
            }
        }

        private void CleanUpCode()
        {
            txbOutput.Clear();
            if (!ReadFile(txbInputFile.Text, out List<string> dmisFile))
            {
                return;
            }

            if (!GetDmisFeaturesFromFile(dmisFile, out List<DmisFeature> features))
            {
                return;
            }

            if (!GetFeaturePathsFromFile(dmisFile, features))
            {
                return;
            }

            if (!GetSettingsFromFile(dmisFile, features))
            {
                return;
            }

            if (!GetClearanceMovesFromFile(dmisFile, features))
            {
                return;
            }

            if (!GetPaMeasFromfFile(dmisFile, features))
            {
                return;
            }

            List<string> outputList = new List<string>();
            
            foreach(DmisFeature df in features)
            {
                List<string> featureOutput = (List<string>)df.CreateNewOutput();
                if (featureOutput == null)
                {
                    MessageBox.Show("Error creating output", "Error", MessageBoxButton.OK,MessageBoxImage.Error);
                    return;
                }
                outputList.AddRange(df.CreateNewOutput());
            }
                

            outputList.Add("ENDFIL");

            string output = string.Join("\r\n", outputList);
            txbOutput.Text = output;

        }

        private bool ReadFile(string fileText, out List<string> dmisFile)
        {
            try
            {
                dmisFile = fileText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                dmisFile.RemoveAll(l => l.Trim().StartsWith("$$"));


                
                return true;
            }
            catch (Exception ex)
            {
                dmisFile = null;
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool GetDmisFeaturesFromFile(List<string> dmisFile, out List<DmisFeature> features)
        {

            try
            {
                features = new List<DmisFeature>();

                Regex featurePattern = new Regex(@"^F\(.*\)=FEAT/");

                List<string> featureText = dmisFile.FindAll(l => featurePattern.IsMatch(l.Trim()));
                foreach (string ft in featureText)
                {
                    if (!DmisFeature.CreateFromString(ft, out DmisFeature newFeature))
                        return false;
                    features.Add(newFeature);
                }

                return true;
            }
            catch (Exception ex)
            {
                features = null;
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool GetFeaturePathsFromFile(List<string> dmisFile, List<DmisFeature> features)
        {
            try
            {
                foreach (DmisFeature df in features)
                {
                    df.GetPathsFromFile(dmisFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool GetSettingsFromFile(List<string> dmisFile, List<DmisFeature> features)
        {
            try
            {
                foreach (DmisFeature df in features)
                {
                    df.GetSettings(dmisFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool GetClearanceMovesFromFile(List<string> dmisFile, List<DmisFeature> features)
        {
            try
            {
                for (int i = 0; i < features.Count; i++)
                {
                    features[i].GetPreMoves(dmisFile, i == 0);
                    features[i].GetPostMoves(dmisFile, i == features.Count - 1);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool GetPaMeasFromfFile(List<string> dmisFile, List<DmisFeature> features)
        {
            try
            {
                foreach (DmisFeature df in features)
                {
                    df.GetPAMeas(dmisFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private void miSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Save DMIS File",
                Filter = "DMIS Files (*.dmi)|*.dmi"
            };

            if ((bool)sfd.ShowDialog())
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(txbOutput.Text);
                    sw.Flush();
                    sw.Close();
                }
            }
        }

        private void miClose_Click(object sender, RoutedEventArgs e)
        {
            txbInputFile.Text = string.Empty;
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
