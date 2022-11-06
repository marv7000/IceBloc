using System.Collections.Generic;

namespace IceBloc.Utility;

/// <summary>
/// Links error codes with a description.
/// </summary>
public class Error
{
    public static Dictionary<int, string> KeyValuePairs = new Dictionary<int, string>()
    {
        {0, "Unknown error occured." },
        {1, "Couldn't get a the requested field from the DbObject!" },
    };
}
