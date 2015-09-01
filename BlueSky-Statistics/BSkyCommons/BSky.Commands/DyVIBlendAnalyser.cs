using System.Collections.Generic;
using BSky.Interfaces.Commands;
using BSky.Interfaces.Model;
using System.Windows.Documents;
using System.Windows;
using BSky.XmlDecoder;

namespace BSky.Commands
{
    public class DyVIBlendAnalyser : ICommandAnalyser
    {
        public CommandOutput Decode(AnalyticsData analysisdata)
        {
            CommandOutput op = new CommandOutput();

            Paragraph Title = new Paragraph();

            OutputHelper.AnalyticsData = analysisdata;
            OutputReader reader = new OutputReader();
            if (analysisdata.Result.CommandString != null && analysisdata.Result.CommandString.Contains("UAloadDataset"))
            {
                reader.Hdr = "Open Dataset"; //21Oct2013
            }
            else
            {
                reader.Hdr = analysisdata.Result.CommandString;
            }
            List<DependencyObject> objs = reader.GetOutput(analysisdata.OutputTemplate);
            op.AddRange(objs);
            return op;
        }
    }
}
