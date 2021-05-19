using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CleanUpPlanningSuiteCode
{
    class Program
    {
        static void Main(string[] args)
        {

            if (!ReadFile(args[0], out List<string> dmisFile))
            {
                ErrorExit();
                return;
            }

            if (!GetDmisFeaturesFromFile(dmisFile, out List<DmisFeature> features))
            {
                ErrorExit();
                return;
            }

            if(!GetFeaturePathsFromFile(dmisFile, features))
            {
                ErrorExit();
                return;
            }

            if(!GetSettingsFromFile(dmisFile, features))
            {
                ErrorExit();
                return;
            }

            if(!GetClearanceMovesFromFile(dmisFile, features))
            {
                ErrorExit();
                return;
            }

            if(!GetPaMeasFromfFile(dmisFile, features))
            {
                ErrorExit();
                return;
            }

            FileInfo inputFile = new FileInfo(args[0]);
            string outputFileName = inputFile.FullName.Replace(inputFile.Extension,$"_clean{inputFile.Extension}");

            if (!WriteNewFile(features, outputFileName))
            {
                ErrorExit();
                return;
            }

        }

        

        private static bool WriteNewFile(List<DmisFeature> features, string outputFileName)
        {
            try
            {
                List<string> outputFile = new List<string>();
                foreach (DmisFeature df in features)
                    outputFile.AddRange(df.CreateNewOutput());

                using (StreamWriter sw = new StreamWriter(outputFileName))
                {
                    foreach (string line in outputFile)
                        sw.WriteLine(line);
                    sw.Flush();
                    sw.Close();
                }
                    return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }





        /// <summary>
        /// Reads the specified file to a list of strings
        /// </summary>
        /// <param name="fileName">The file to read</param>
        /// <param name="dmisFile">The resulting file.</param>
        /// <returns>True if successful, false otherwise.</returns>
        private static bool ReadFile(string fileName, out List<string> dmisFile)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    Console.WriteLine($"Could not find \"{fileName}\"");
                    dmisFile = null;
                    return false;
                }
                dmisFile = new List<string>();
                using (StreamReader sr = new StreamReader(fileName))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if(!line.StartsWith("$$") && !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
                            dmisFile.Add(line);
                    }
                    sr.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                dmisFile = null;
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private static void ErrorExit()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static bool GetDmisFeaturesFromFile(List<string> dmisFile, out List<DmisFeature> features)
        {
            
            try
            {
                features = new List<DmisFeature>();

                Regex featurePattern = new Regex(@"^F\(.*\)=FEAT/");

                List<string> featureText = dmisFile.FindAll(l => featurePattern.IsMatch(l.Trim()));
                foreach(string ft in featureText)
                {
                    if(!DmisFeature.CreateFromString(ft, out DmisFeature newFeature))
                        return false;
                    features.Add(newFeature);
                }

                return true;
            }
            catch(Exception ex)
            {
                features = null;
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private static bool GetFeaturePathsFromFile(List<string> dmisFile, List<DmisFeature> features)
        {
            try
            {
                foreach (DmisFeature df in features)
                {
                    df.GetPathsFromFile(dmisFile);
                }
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private static bool GetSettingsFromFile(List<string> dmisFile, List<DmisFeature> features)
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

        private static bool GetClearanceMovesFromFile(List<string> dmisFile, List<DmisFeature> features)
        {
            try
            {
                for(int i = 0; i < features.Count; i++)
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

        private static bool GetPaMeasFromfFile(List<string> dmisFile, List<DmisFeature> features)
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

    }
}
