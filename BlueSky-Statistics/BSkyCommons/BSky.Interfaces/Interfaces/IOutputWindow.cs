using BSky.Interfaces.Model;

namespace BSky.Interfaces.Interfaces
{
    public interface IOutputWindow
    {
        string WindowName
        { get; set; }

        void Show();
        void AddAnalyis(AnalyticsData data);
        //No need void AddAnalyisFromFile(string fullpathfilename);//30May2012
        //void SaveAnalysisBinary();
        //void LoadAnalysisBinary();
    }
}
