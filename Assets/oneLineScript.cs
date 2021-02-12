using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class oneLineScript : MonoBehaviour
{
    //public stuff
    public KMAudio Audio;
    public KMSelectable RefNode;
    public MeshRenderer RefLED;
    public MeshRenderer RefWire;
    public List<KMSelectable> Buttons;
    public List<MeshRenderer> LEDs;
    public List<MeshRenderer> Wires;
    public TextMesh Text;
    public KMBombModule Module;

    //private stuff
    private List<bool> hl = new List<bool> { };
    private int selected = 6;
    private List<int[]> wiredata = new List<int[]> { };
    private List<int> solution;
    private bool solved;

    void Awake()
    {
        for (int i = 0; i < 6; i++)
        {
            Buttons.Add(Instantiate(RefNode, Module.transform));
            Module.GetComponent<KMSelectable>().Children[i] = Buttons[i];
            LEDs.Add(Instantiate(RefLED, Module.transform));
            hl.Add(false);
            int x = i;
            Buttons[i].OnHighlight += delegate { hl[x] = true; };
            Buttons[i].OnHighlightEnded += delegate { hl[x] = false; };
            Buttons[i].OnInteract += delegate
            {
                if (!solved)
                {
                    Buttons[x].AddInteractionPunch(1f);
                    if (selected == 6)
                    {
                        selected = x;
                        Audio.PlaySoundAtTransform("Select", Buttons[x].transform);
                    }
                    else if (selected == x)
                    {
                        selected = 6;
                        Audio.PlaySoundAtTransform("Deselect", Buttons[x].transform);
                    }
                    else
                    {
                        if (wiredata.Any(y => y.Contains(x) && y.Contains(selected)))
                        {
                            int idx = wiredata.IndexOf(wiredata.Where(y => y.Contains(x) && y.Contains(selected)).First());
                            wiredata.RemoveAt(idx);
                            Wires[idx].enabled = false;
                            Wires.RemoveAt(idx);
                            Audio.PlaySoundAtTransform("Disconnect", Buttons[x].transform);
                        }
                        else if (wiredata.Count(y => y.Contains(x)) < 2 && wiredata.Count(y => y.Contains(selected)) < 2)
                        {
                            Wires.Add(Instantiate(RefWire, Module.transform));
                            float deltax = Buttons[x].transform.localPosition.x - Buttons[selected].transform.localPosition.x;
                            float deltay = Buttons[x].transform.localPosition.z - Buttons[selected].transform.localPosition.z;
                            Wires.Last().transform.localEulerAngles = new Vector3(0, -Mathf.Atan(deltay / deltax) * 57.2957795f, 0);
                            Wires.Last().transform.localScale = new Vector3(Pytha(deltax, deltay), .001f, .01f);
                            Wires.Last().transform.localPosition = Vector3.Lerp(Buttons[x].transform.localPosition, Buttons[selected].transform.localPosition, 0.5f);
                            Wires.Last().enabled = true;
                            wiredata.Add(new int[] { selected, x });
                            Audio.PlaySoundAtTransform("Connect", Buttons[x].transform);
                        }
                        else
                            Audio.PlaySoundAtTransform("Deselect", Buttons[x].transform);
                        for (int j = 0; j < 6; j++)
                            Buttons[j].GetComponent<MeshRenderer>().material.color = new Color(.5f, .4375f - .0625f * wiredata.Count(y => y.Contains(j)), .25f);
                        selected = 6;
                        CheckSolve();
                    }
                }
                return false;
            };
        }
        Placement();
        RefNode.GetComponent<Renderer>().enabled = false;
        RefLED.enabled = false;
        RefWire.enabled = false;
    }

    void Start()
    {
        List<List<int>> permA = new List<List<int>> { new List<int> { } };
        for (int i = 0; i < 6; i++)
        {
            List<List<int>> permB = new List<List<int>> { };
            foreach (List<int> set in permA)
                for (int j = 0; j < 6; j++)
                    if (!set.Contains(j))
                        permB.Add(set.Concat(new List<int> { j }).ToList());
            permA = permB;
        }
        solution = permA.OrderBy(x => PathLength(x)).First();
    }

    void Update()
    {
        if (!solved)
        {
            for (int i = 0; i < 6; i++)
                if (selected == i)
                    LEDs[i].material.color = new Color(1, .825f - .125f * wiredata.Count(x => x.Contains(i)), .5f);
                else if (hl[i])
                    LEDs[i].material.color = new Color(1, .75f - .25f * wiredata.Count(x => x.Contains(i)), 0);
                else
                    LEDs[i].material.color = new Color(0, 0, 0);
        }
    }

    private float Pytha(float x, float y)
    {
        return Mathf.Pow(x * x + y * y, 0.5f);
    }

    private void Placement()
    {
        for (int i = 0; i < 6; i++)
        {
            bool notgood = true;
            Buttons.Add(RefNode);
            while (notgood)
            {
                Buttons[i].transform.localPosition = new Vector3(Rnd.Range(-0.065f, 0.065f), 0.015f, Rnd.Range(-0.065f, 0.065f));
                notgood = false;
                for (int j = 0; j < Buttons.Count(); j++)
                    if (i != j && Pytha((Buttons[j].transform.localPosition.x - Buttons[i].transform.localPosition.x) * 100f, (Buttons[j].transform.localPosition.z - Buttons[i].transform.localPosition.z) * 100f) <= 3f)
                        notgood = true;
            }
            Buttons.RemoveAt(6);
            LEDs[i].transform.localPosition = Buttons[i].transform.localPosition;
        }
    }

    private float PathLength (List<int> arrangement)
    {
        float rt = 0f;
        for (int i = 0; i < arrangement.Count() - 1; i++)
            rt += Pytha((Buttons[arrangement[i]].transform.localPosition.x - Buttons[arrangement[i + 1]].transform.localPosition.x) * 100f, (Buttons[arrangement[i]].transform.localPosition.z - Buttons[arrangement[i + 1]].transform.localPosition.z) * 100f);
        return rt;
    }

    private void CheckSolve()
    {
        if (wiredata.Count() == 5)
        {
            bool safe = true;
            for (int i = 0; i < 5; i++)
                if (!wiredata.Any(x => x.Contains(solution[i]) && x.Contains(solution[i + 1])))
                    safe = false;
            if (safe)
            {
                solved = true;
                Module.HandlePass();
                for (int j = 0; j < 6; j++)
                {
                    Buttons[j].GetComponent<MeshRenderer>().material.color = new Color(.4375f, .5f, .375f);
                    LEDs[j].material.color = new Color(0, 1, 0);
                }
                foreach (var wire in Wires)
                    wire.material.color = new Color(0, 1, 0);
                Audio.PlaySoundAtTransform("Solve", Module.transform);
            }
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} press 1 2 3 4 5 6' to press those positions (ordered from top to bottom).";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (Regex.IsMatch(command, @"^press\s((1|2|3|4|5|6)(\s?))+$"))
        {
            MatchCollection matches = Regex.Matches(command.Replace("press", ""), @"(1|2|3|4|5|6)");
            foreach (Match match in matches)
                foreach (Capture capture in match.Captures)
                {
                    Debug.Log(capture.ToString());
                    Buttons.OrderByDescending(x => x.transform.localPosition.z).ToList()[capture.ToString()[0] - '1'].OnInteract();
                    yield return null;
                }
            yield return "solve";
        }
        else
            yield return "sendtochaterror Invalid command.";
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        while (wiredata.Count() > 0)
            for (int i = 0; i < 2; i++)
            {
                Buttons[wiredata.First()[i]].OnInteract();
                yield return true;
            }
        for (int i = 0; i < solution.Count() - 1; i++)
            for (int j = 0; j < 2; j++)
            {
                Buttons[solution[i + j]].OnInteract();
                yield return true;
            }
        yield return true;
    }
}