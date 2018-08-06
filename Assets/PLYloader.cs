
using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PLYloader
    {
    float minX, maxX, minY, maxY, minZ, maxZ;

    public Mesh LoadFile(string file, bool dense)
    {

        Vector3 center = new Vector3();
        //file = Regex.Replace(file, @" +", " ");
        int numberOfVertices = 0;
        StringReader strReader = new StringReader(file);
        while (true)
        {

            string line = strReader.ReadLine();
            if (line == null || line == "end_header") break;
            if (line.Length == 0) continue;
            if (line.StartsWith("element vertex "))
            {
                string[] v = line.Substring(0).Split(' ');

                numberOfVertices = Int32.Parse(v[2]);

                //Debug.Log("\n" + numberOfVertices + " vertices to draw\n");
            }

        }

        Mesh model = new Mesh();
        //model.colors = new Color[numberOfVertices];
        // model.vertices = new Vector3[numberOfVertices];
        Vector3[] vertices = new Vector3[numberOfVertices];
        Color[] colors = new Color[numberOfVertices];
        int[] indecies = new int[numberOfVertices];

        for (int i = 0; i < numberOfVertices; i++)
        {
            string line = strReader.ReadLine();
            if (line == null || line.Length == 0)
            {
                Debug.Log("There's something wrong with the file");
                break;
            }


            string[] v = line.Substring(0).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);


            try
            {
                vertices[i] = new Vector3(float.Parse(v[0]), float.Parse(v[1]), float.Parse(v[2]));
            }
            catch(Exception e)
            {
                Debug.Log(v.Length);
                Debug.Log(float.Parse(v[0]) +", "+ float.Parse(v[1]) + ", " + float.Parse(v[2]));
            }

            center += vertices[i];

            CheckMinMax(vertices[i], i);

            if (dense)
            {
                colors[i] = new Color(float.Parse(v[6]), float.Parse(v[7]), float.Parse(v[8]));
            }
            else
            {

                colors[i] = new Color(float.Parse(v[3]), float.Parse(v[4]), float.Parse(v[5]));
                //Debug.Log(float.Parse(v[3]) + " " + float.Parse(v[4]) + " " + float.Parse(v[5]));
            }



            indecies[i] = i;

        }

        Debug.Log("All data read");

        model.vertices = vertices;
        model.colors = colors;

        model.SetIndices(indecies, MeshTopology.Points, 0);

        GameObject reconstruction = GameObject.Find("ReconstructionObject");

        BoxCollider collider = reconstruction.GetComponent<BoxCollider>();

        collider.size = new Vector3(Math.Abs(maxX - minX), Math.Abs(maxY - minY), Math.Abs(maxZ - minZ));

        collider.center = center / numberOfVertices;

        return numberOfVertices == 0 ? null : model;
    }

    void CheckMinMax(Vector3 coords, int i) {
        if (i == 0) {
            minX = coords.x;
            maxX = coords.x;
            minY = coords.y;
            maxY = coords.y;
            minZ = coords.z;
            maxZ = coords.z;
        }
        else
        {
            if (coords.x < minX)
                minX = coords.x;
            else
                maxX = coords.x;

            if (coords.y < minY)
                minY = coords.y;
            else
                maxY = coords.y;

            if (coords.z < minZ)
                minZ = coords.z;
            else
                maxZ = coords.z;
        }

    }

    }

