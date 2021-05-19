using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CleanUpPlanningSuiteCode
{
    internal class DmisFeature
    {
        private string m_name;
        private string m_featureData;
        private List<string> m_paths = new List<string>();
        private string m_paMeas;
        private List<string> m_scanSettings = new List<string>();
        private List<string> m_preMoves = new List<string>();
        private List<string> m_postMoves = new List<string>();
        public string Name
        {
            get { return m_name; }
            set
            {
                if (m_name == value) return;
                m_name = value;
            }
        }

        private DmisFeature() { }

        internal static bool CreateFromString(string ft, out DmisFeature newFeature)
        {
            newFeature = new DmisFeature();
            try
            {
                if(!GetFeatureName(ft, out string name))
                {
                    newFeature = null;
                    return false;
                }
                newFeature.Name = name;
                newFeature.m_featureData = ft.Split(new char[] { '=' })[1];
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                newFeature = null;
                return false;
            }
            

        }

        internal IEnumerable<string> CreateNewOutput()
        {
            try
            {
                List<string> output = new List<string>();
                string featureType = GetFeatureType();
                string newFeatureName = Name.Replace("_Scan", string.Empty);
                output.Add($"$$<MEAS_{featureType} name = \"{newFeatureName}\">");
                foreach (string line in m_paths)
                    output.Add($"  {line.Replace("_Scan", string.Empty)}");
                output.Add("  MODE/PROG,MAN");
                output.Add($"  F({newFeatureName})={m_featureData}");
                output.Add($"  MEAS/{featureType},F({newFeatureName}),{(featureType == "PLANE" ? 3 : 2)}");
                foreach (string line in m_scanSettings)
                {
                    output.Add($"    {line}");
                }
                foreach (string line in m_preMoves)
                {
                    output.Add($"    {line}");
                }
                output.Add($"    {m_paMeas.Replace("_Scan", string.Empty)}");
                foreach (string line in m_postMoves)
                {
                    output.Add($"    {line}");
                }
                output.Add("  ENDMES");
                output.Add($"$$<\\MEAS_{featureType} = {newFeatureName}>");
                output.Add(string.Empty);
                return output;
            }
            catch 
            {
                return null;
            }
        }

        private string GetFeatureType()
        {
            return m_featureData.Split(new[] { ',','/'})[1];
        }

        private static bool GetFeatureName(string ft, out string name)
        {
            try
            {
                string feature = ft.Split(new char[] { '=' })[0];
                name = feature.Replace("F(",string.Empty).Replace(")",string.Empty);
                return true;
            }
            catch(Exception ex)
            {
                name = null;
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        internal void GetPathsFromFile(List<string> dmisFile)
        {
            Regex pathRegex = new Regex(@$"^P\(PATH_{Name}.*\)");
            List<string> paths = dmisFile.FindAll(l => pathRegex.IsMatch(l.Trim()));
            m_paths.AddRange(paths);
        }

        internal void GetSettings(List<string> dmisFile)
        {
            try
            {
                int measureStart = dmisFile.FindIndex(l => Regex.IsMatch(l.Trim(), $@"^MEAS/.*F\({Name}\).*"));
                string line = string.Empty;
                int lineNumber = measureStart;
                while (lineNumber >= 0 && !(line = dmisFile[lineNumber--]).StartsWith("SNSET")) ;
                if (lineNumber >= 0)
                {
                    lineNumber++;
                    for (int i = lineNumber - 2; i <= lineNumber; i++)
                        m_scanSettings.Add(dmisFile[i]);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        internal void GetPreMoves(List<string> dmisFile, bool isFirst = false)
        {
            int measureStart = dmisFile.FindIndex(l => Regex.IsMatch(l.Trim(), $@"^MEAS/.*F\({Name}\).*"));
            string line = string.Empty;
            int lineNumber = measureStart;
            while (lineNumber >= 0 && !(line = dmisFile[lineNumber--]).StartsWith("GOTO")) ;
            lineNumber++;
            while(lineNumber > 0 && (line = dmisFile[lineNumber--]).StartsWith("GOTO"))
                m_preMoves.Add(line);

            m_preMoves.Reverse();
            if (!isFirst)
            {
                int numberOfMoves = m_preMoves.Count;
                for (int i = 0; i < numberOfMoves / 2; i++)
                    m_preMoves.RemoveAt(0);
            }
        }

        internal void GetPostMoves(List<string> dmisFile, bool isLast = false)
        {
            int measureStart = dmisFile.FindIndex(l => Regex.IsMatch(l.Trim(), $@"^MEAS/.*F\({Name}\).*"));
            string line = string.Empty;
            int lineNumber = measureStart;
            while (lineNumber >= 0 && !(line = dmisFile[lineNumber++]).StartsWith("GOTO")) ;
            lineNumber--;
            while (lineNumber > 0 && (line = dmisFile[lineNumber++]).StartsWith("GOTO"))
                m_postMoves.Add(line);

            if (!isLast)
            {
                int numberOfMoves = m_postMoves.Count;
                for (int i = 0; i < numberOfMoves / 2; i++)
                    m_postMoves.RemoveAt(m_postMoves.Count-1);
            }
        }

        internal void GetPAMeas(List<string> dmisFile)
        {
            List<string> pathNames = new List<string>();
            for(int i = 0; i < m_paths.Count; i++)
            {
                string pathName = m_paths[i].Split(new[] { '=' })[0];
                pathNames.Add(pathName);
            }

            m_paMeas = dmisFile.Find(l =>
            {
                if (!l.StartsWith("PAMEAS")) return false;
                foreach (string path in pathNames)
                    if (!l.Contains(path)) return false;

                return true;
            });
        }
    }
}