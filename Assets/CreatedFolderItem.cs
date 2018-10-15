using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//class to get the name of the folder from JSON
[Serializable]
public class CreatedFolderItem
{
    public string folder;

    public CreatedFolderItem(string f)
    {
        folder = f;
    }
}