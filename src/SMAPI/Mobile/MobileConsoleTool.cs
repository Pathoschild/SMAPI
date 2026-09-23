using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewModdingAPI.Mobile;

public static class MobileConsoleTool
{
    static string _currentLine = "";
    static string CurrentLine
    {
        get
        {
            lock (_currentLine)
                return _currentLine;
        }
        set
        {
            lock (_currentLine)
            {
                _currentLine = value;
                if (_currentLine == null)
                    _currentLine = ""; //safe null value
            }
        }
    }
    public static void WriteLine(string line)
    {
        CurrentLine = line;
    }
    public static string ReadLine()
    {
        //wait current line message
        while (string.IsNullOrEmpty(CurrentLine)) { }

        string tempLine = CurrentLine;
        CurrentLine = ""; //clear after read it
        return tempLine;
    }
}
