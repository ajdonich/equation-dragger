using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class DataElement 
{
    public Dictionary<string,Fraction> elemValues;
    public Dictionary<string,Vector3> elemPositions;
}

public abstract class VisualElement
{
    public static int FONT_SIZE = 25;
    public static int MINI_FONT_SIZE = 20;

    public GameObject rootVisElemGo;
    public Vector2 bounds  { get; protected set; }
    public Vector3 rootPos { get => rootVisElemGo.transform.position;}

    public Dictionary<string,(GameObject,Text)> goTextObjects;
    public Dictionary<string,Vector3> offsets;


    // Calulates and set bounds, offsets and Collider sizes 
    public abstract void calculateBounds();

    // Expects initialized bounds/offsets 
    public void setPosition(Vector3 pos) {
        rootVisElemGo.transform.position = pos;
        foreach (string key in goTextObjects.Keys) {
            (GameObject tgo, Text tcomp) tup = goTextObjects[key];
            if (tup.tgo.activeSelf) {
                tup.tgo.transform.position = pos + offsets[key];
                if (tup.tcomp == null) {
                    Vector3 start = pos + offsets["frac-line-start"];
                    Vector3 end = pos + offsets["frac-line-end"];
                    tup.tgo.GetComponent<LineRenderer>().SetPositions(
                        new Vector3[]{start, end});
                }
            }
        }
    }

    public void setTextAlpha(float alpha) {
        foreach (string key in goTextObjects.Keys) {
            (GameObject tgo, Text tcomp) tup = goTextObjects[key];
            if (tup.tgo.activeSelf) {
                if (tup.tcomp == null) {
                    LineRenderer lrComp = tup.tgo.GetComponent<LineRenderer>();
                    lrComp.startColor = lrComp.endColor = new Color(0.0f, 0.0f, 0.0f, alpha);
                }
                else tup.tcomp.color = new Color(0.0f, 0.0f, 0.0f, alpha);
            }

        }
    }

    public void setBgColor(Color col) {
        rootVisElemGo.GetComponent<UnityEngine.UI.Image>().color = col;
        // rootVisElemGo.GetComponent<UnityEngine.UI.Image>().color = MouseHandler.highlight;
    }

    public bool contains(GameObject go) {
        foreach (string key in goTextObjects.Keys) {
            (GameObject tgo, Text tcomp) tup = goTextObjects[key];
            if (tup.tgo == go) return true;
        }

        return false;
    }

    public void setActive(bool value) {
        rootVisElemGo.SetActive(value);
        foreach (string key in goTextObjects.Keys) {
            (GameObject tgo, Text tcomp) tup = goTextObjects[key];
            tup.tgo.SetActive(value);
        }
    }

    public void destroy() {
        // Note: GameObject operator !=(null) overloaded to detect Destroyed.
        foreach (string key in goTextObjects.Keys) {
            (GameObject tgo, Text tcomp) tup = goTextObjects[key];
            if (tup.tgo != null) Object.Destroy(tup.tgo);
        }
        Object.Destroy(rootVisElemGo);
    }

    private static TextGenerator generator = new TextGenerator();
    protected static Vector2 _getPreferredSize(Text textComp) {
        TextGenerationSettings settings = textComp.GetGenerationSettings(new Vector2(500.0f, 500.0f));
        return new Vector2(generator.GetPreferredWidth(textComp.text, settings), 
                           generator.GetPreferredHeight(textComp.text, settings));
    }

    protected static GameObject _createVisElemGo(string name) {
        GameObject visElemGo = new GameObject(name);
        Image background = visElemGo.AddComponent<Image>();
        background.color = Color.clear;
        // background.color = MouseHandler.highlight;
        return visElemGo;
    }

    protected void _addTextGo(GameObject parent, string name, 
        string text, int fontSize, FontStyle fStyle=FontStyle.Bold)
    {
        GameObject textGo = new GameObject(name); 
        textGo.transform.SetParent(parent.transform);
        textGo.AddComponent<MouseHandler>();

        // This will set RectTransform sizes (eventually, during layout) 
        ContentSizeFitter csf = textGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Collider and Rigidbody needed for drag'n drop 
        Rigidbody2D rbComp = textGo.AddComponent<Rigidbody2D>();
        rbComp.bodyType = RigidbodyType2D.Kinematic;
                
        BoxCollider2D collComp = textGo.AddComponent<BoxCollider2D>();
        collComp.isTrigger = true;

        Text textComp = textGo.AddComponent<Text>();
        textComp.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.fontStyle = fStyle;
        textComp.fontSize = fontSize;
        textComp.color = Color.black;
        textComp.text = text;

        goTextObjects.Add(name, (textGo, textComp));
    }

    protected void _addLineGo(GameObject parent, string name)
    {
        GameObject lineGo = new GameObject(name); 
        lineGo.transform.SetParent(parent.transform);
        lineGo.AddComponent<RectTransform>();
        lineGo.AddComponent<MouseHandler>();

        // Collider and Rigidbody needed for drag'n drop 
        Rigidbody2D rbComp = lineGo.AddComponent<Rigidbody2D>();
        rbComp.bodyType = RigidbodyType2D.Kinematic;
                
        BoxCollider2D collComp = lineGo.AddComponent<BoxCollider2D>();
        collComp.isTrigger = true;

        LineRenderer lrComp = lineGo.AddComponent<LineRenderer>();
        lrComp.material = new Material(Shader.Find("Sprites/Default"));
        lrComp.startColor = lrComp.endColor = Color.black;
        lrComp.startWidth = lrComp.endWidth = 1.5f;
        lrComp.positionCount = 2;

        goTextObjects.Add(name, (lineGo, null));
    }

    // Debug
    public void printOffsets() {
        string buffer = $"{rootVisElemGo.name}: ";
        foreach (string key in offsets.Keys)
            buffer += $"{key} {offsets[key]}, ";

        Debug.Log(buffer);
    }
}


public class FracVisElem : VisualElement
{
    public FracVisElem(GameObject parent, Fraction rat, bool hideSign, bool hideOne) {
        goTextObjects = new Dictionary<string,(GameObject,Text)>();
        rootVisElemGo = VisualElement._createVisElemGo(rat.ToString());
        rootVisElemGo.transform.SetParent(parent.transform);

        int fontSize = rat.den == 1 ? FONT_SIZE : MINI_FONT_SIZE;
        _addTextGo(rootVisElemGo, "frac-sign", "", FONT_SIZE);
        _addTextGo(rootVisElemGo, "frac-num", "", fontSize);
        _addLineGo(rootVisElemGo, "frac-line");
        _addTextGo(rootVisElemGo, "frac-den", "", MINI_FONT_SIZE);
        update(rat, hideSign, hideOne);
    }

    public void update(Fraction rat, bool hideSign, bool hideOne) {
        (GameObject tgo, Text tcomp) tupsign = goTextObjects["frac-sign"];
        (GameObject tgo, Text tcomp) tupnum = goTextObjects["frac-num"];
        (GameObject tgo, Text tcomp) tupline = goTextObjects["frac-line"];
        (GameObject tgo, Text tcomp) tupden = goTextObjects["frac-den"];

        rootVisElemGo.name = rat.ToString();
        string sign = rat.num >= 0 ? " + " : " - ";
        if (hideSign && rat.num >= 0) sign = " ";
        tupsign.tcomp.text = sign;

        if (rat.den == 1) { // Integer-like-state
            bool isHiddenOne = rat == 1 && hideOne;
            tupnum.tgo.SetActive(!isHiddenOne);
            tupline.tgo.SetActive(false);
            tupden.tgo.SetActive(false);

            if (!isHiddenOne) {
                tupnum.tcomp.fontSize = FONT_SIZE;
                tupnum.tcomp.text = $"{System.Math.Abs(rat.num)}";
            }
        }
        else { // True-fraction-state
            tupnum.tgo.SetActive(true);
            tupline.tgo.SetActive(true);
            tupden.tgo.SetActive(true);

            tupnum.tcomp.fontSize = MINI_FONT_SIZE;
            tupnum.tcomp.text = $"{System.Math.Abs(rat.num)}";
            tupden.tcomp.text = $"{rat.den}";
        }
    }

    public override void calculateBounds() {
        if (offsets == null) offsets = new Dictionary<string,Vector3>();
        (GameObject tgo, Text tcomp) tupsign = goTextObjects["frac-sign"];
        (GameObject tgo, Text tcomp) tupnum = goTextObjects["frac-num"];
        (GameObject tgo, Text tcomp) tupline = goTextObjects["frac-line"];
        (GameObject tgo, Text tcomp) tupden = goTextObjects["frac-den"];

        Vector2 szsign = VisualElement._getPreferredSize(tupsign.tcomp);
        Vector2 sznum = VisualElement._getPreferredSize(tupnum.tcomp);
        Vector2 szden = VisualElement._getPreferredSize(tupden.tcomp);
        Vector2 szline = new Vector2(Mathf.Max(sznum.x, szden.x) + 2, 2.0f);

        tupsign.tgo.GetComponent<BoxCollider2D>().size = szsign;
        tupnum.tgo.GetComponent<BoxCollider2D>().size = sznum;
        tupline.tgo.GetComponent<BoxCollider2D>().size = szline;
        tupden.tgo.GetComponent<BoxCollider2D>().size = szden;

        if (tupden.tgo.activeSelf) {
            offsets["frac-sign"] = Vector3.left * (szline.x/2.0f);
            offsets["frac-line"] = Vector3.right * (szsign.x/2.0f);
            offsets["frac-num"] = (Vector3.up * ((szline.y + sznum.y)/2.0f)) + offsets["frac-line"];
            offsets["frac-den"] = (Vector3.down * ((szline.y + szden.y)/2.0f)) + offsets["frac-line"];
            offsets["frac-line-start"] = offsets["frac-line"] + (Vector3.left * szline.x/2.0f);
            offsets["frac-line-end"] = offsets["frac-line"] + (Vector3.right * szline.x/2.0f);
            bounds = new Vector2(szsign.x + szline.x, sznum.y + szline.y + szden.y);
        }
        else if (tupnum.tgo.activeSelf) {            
            offsets["frac-sign"] = Vector3.left * (sznum.x/2.0f);
            offsets["frac-num"] = Vector3.right * (szsign.x/2.0f);
            bounds = new Vector2(szsign.x + sznum.x, Mathf.Max(sznum.y, szsign.y));
        }
        else {
            offsets["frac-sign"] = Vector3.zero;
            bounds = szsign;
        }

        rootVisElemGo.GetComponent<RectTransform>().sizeDelta = bounds;
    }
}


public class VarVisElem : VisualElement 
{
    public VarVisElem(GameObject parent, string variable) {
        goTextObjects = new Dictionary<string,(GameObject,Text)>();
        rootVisElemGo = VisualElement._createVisElemGo(variable);
        rootVisElemGo.transform.SetParent(parent.transform);
        _addTextGo(rootVisElemGo, "var-var", variable, 25);
    }

    public override void calculateBounds() {
        if (offsets == null) offsets = new Dictionary<string,Vector3>();
        offsets["var-var"] = Vector3.zero;

        (GameObject tgo, Text tcomp) tupvar = goTextObjects["var-var"];
        bounds = VisualElement._getPreferredSize(tupvar.tcomp);
        tupvar.tgo.GetComponent<BoxCollider2D>().size = bounds;        
        rootVisElemGo.GetComponent<RectTransform>().sizeDelta = bounds;
    }
}

