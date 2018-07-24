using Unity;
using System;

[Serializable]
public class SceneItem
{
    public string description;

    public SceneItem(string desc)
    {
        description = desc;
    }
}

[Serializable]
public class CreatedSceneItem
{
    public int id;
}