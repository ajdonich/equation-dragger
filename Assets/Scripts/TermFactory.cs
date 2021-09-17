using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermFactory
{
    public static AbstractTerm CreateTerm(int value, int denominator=1, bool dropSign=false) {
        ConstantTerm constTerm = new ConstantTerm(value, denominator);
        constTerm.instantiate(dropSign);
        return constTerm;
    }

    public static AbstractTerm CreateZeroTerm(bool dropSign=true) {
        ConstantTerm constTerm = new ConstantTerm(0, 1);
        constTerm.instantiate(dropSign);
        return constTerm;
    }

    public static AbstractTerm CreateTerm(string variable, int value, int denominator=1, bool dropSign=false) {
        VariableTerm varTerm = new VariableTerm(variable, value, denominator);
        varTerm.instantiate(dropSign);
        return varTerm;
    }

    public static Text getTextComp(GameObject termGo) {
        return termGo.transform.Find("Text").GetComponent<Text>();
    }

    public static Vector2 getPreferredSize(Text textComp) {
        TextGenerator generator = new TextGenerator();            
        TextGenerationSettings settings = textComp.GetGenerationSettings(new Vector2(500.0f, 500.0f));
        return new Vector2(generator.GetPreferredWidth(textComp.text, settings), 
                           generator.GetPreferredHeight(textComp.text, settings));
    }

    public static GameObject instantiate(string text, bool clickable=true, FontStyle fStyle=FontStyle.Bold) {
        GameObject termObj = new GameObject(text);
        Image background = termObj.AddComponent<Image>();
        background.color = Color.clear;

        VerticalLayoutGroup vlgComp = termObj.AddComponent<VerticalLayoutGroup>();
        vlgComp.childAlignment = TextAnchor.MiddleCenter;
        vlgComp.childControlHeight = true;
        vlgComp.childControlWidth = true;        

        // This will set RectTransform sizes (eventually, during layout) 
        ContentSizeFitter csf = termObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Text is child GameObject (termObj already included Image
        // and only one "graphic" component allowed per GameObject)
        GameObject textChildObj = new GameObject("Text"); 
        textChildObj.transform.SetParent(termObj.transform);

        Text textComp = textChildObj.AddComponent<Text>();
        textComp.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        textComp.fontStyle = fStyle;
        textComp.fontSize = 25;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.black;
        textComp.text = text;

        if (clickable) {            
            // Collider and Rigidbody needed for drag'n drop 
            Rigidbody2D rbComp = termObj.AddComponent<Rigidbody2D>();
            rbComp.bodyType = RigidbodyType2D.Kinematic;
                    
            BoxCollider2D collComp = termObj.AddComponent<BoxCollider2D>();
            collComp.isTrigger = true; 

            termObj.AddComponent<MouseHandler>();
        }
        return termObj;
    }
}

public abstract class AbstractTerm 
{
    public List<GameObject> gameObjects = new List<GameObject>();

    public abstract void instantiate(bool dropSign);
    public abstract void setDropSign(bool dropSign);
    public abstract void setText(int value, int denominator=1);
    public abstract void setValue(int value, int denominator=1);
    public abstract int getValue();

    public GameObject this[int index] {
        get => gameObjects[index];
    }

    public void setParent(Transform parent) {
        foreach (GameObject go in gameObjects)
            go.transform.SetParent(parent);
    }

    public (Vector2 size, Vector2 offset) getBounds(Vector3 lftGoPos) {
        float xoff = lftGoPos.x;
        float x, y; x = y = 0.0f;
        for (int i=0; i<gameObjects.Count; ++i) {
            Vector2 sz = TermFactory.getPreferredSize(TermFactory.getTextComp(gameObjects[i]));
            x += sz.x; y = Mathf.Max(y, sz.y);
            if (i > 0) xoff += sz.x/2;
        }

        return (new Vector2(x,y), new Vector2(xoff, 0.0f));
    }

    public void setTextAlpha(float alpha) {
        foreach (GameObject go in gameObjects)
            TermFactory.getTextComp(go).color = 
                new Color(0.0f, 0.0f, 0.0f, alpha);
    }

    public void setBgColor(Color col) {
        foreach (GameObject go in gameObjects)
            go.GetComponent<UnityEngine.UI.Image>().color = col;
    }
}

public class ConstantTerm : AbstractTerm
{
    public int numerator;
    public int denominator;
    public bool dropSign;

    public ConstantTerm(int numerator, int denominator=1) {
        this.numerator = numerator;
        this.denominator = denominator;
        this.dropSign = false;
    }

    public override void instantiate(bool dropSign=false) {
        this.dropSign = dropSign;
        string sign = numerator >= 0 ? " + " : " - ";
        if (dropSign) sign = numerator < 0 ? "- " : "";
        string text = sign + System.Math.Abs(numerator).ToString();
        gameObjects.Add(TermFactory.instantiate(text));
    }

    public override void setDropSign(bool dropSign) {
        this.dropSign = dropSign;
        string sign = numerator >= 0 ? " + " : " - ";
        if (dropSign) sign = numerator < 0 ? "- " : "";
        string text = sign + System.Math.Abs(numerator).ToString();
        TermFactory.getTextComp(gameObjects[0]).text = text;
    }

    public override void setText(int value, int denominator=1) {
        string sign = value >= 0 ? " + " : " - ";
        if (dropSign) sign = value < 0 ? "- " : "";
        string text = sign + System.Math.Abs(value).ToString();
        TermFactory.getTextComp(gameObjects[0]).text = text;
    }

    public override void setValue(int value, int denominator=1) {
        this.numerator = value;
        this.denominator = denominator;
    }

    public override int getValue() {
        return numerator;
    }
}

public class VariableTerm : AbstractTerm
{
    public int numerator;
    public int denominator;
    public string variable;
    public bool dropSign;

    public VariableTerm(string variable, int numerator, int denominator=1) {
        this.numerator = numerator;
        this.denominator = denominator;
        this.variable = variable;
    }

    public override void instantiate(bool dropSign=false) {
        this.dropSign = dropSign;
        string sign = numerator >= 0 ? " + " : " - ";
        if (dropSign) sign = numerator < 0 ? "- " : " ";
        
        int value = System.Math.Abs(numerator);
        if (value == 1) gameObjects.Add(TermFactory.instantiate(sign));
        else gameObjects.Add(TermFactory.instantiate(sign + value.ToString()));
        gameObjects.Add(TermFactory.instantiate(variable));
    }

    public override void setDropSign(bool dropSign) {
        this.dropSign = dropSign;
        string sign = numerator >= 0 ? " + " : " - ";
        if (dropSign) sign = numerator < 0 ? "- " : " ";
        
        int value = System.Math.Abs(numerator);
        if (value == 1) TermFactory.getTextComp(gameObjects[0]).text = sign;
        else TermFactory.getTextComp(gameObjects[0]).text = sign + value.ToString();
        TermFactory.getTextComp(gameObjects[1]).text = variable;
    }

    public override void setText(int value, int denominator=1) {
        string sign = value >= 0 ? " + " : " - ";
        if (dropSign) sign = value < 0 ? "- " : "";

        int absValue = System.Math.Abs(value);
        if (absValue == 1) TermFactory.getTextComp(gameObjects[0]).text = sign;
        else TermFactory.getTextComp(gameObjects[0]).text = sign + absValue.ToString();
        TermFactory.getTextComp(gameObjects[1]).text = variable;
    }

    public override void setValue(int value, int denominator=1) {
        this.numerator = value;
        this.denominator = denominator;
    }

    public override int getValue() {
        return numerator;
    }
}

