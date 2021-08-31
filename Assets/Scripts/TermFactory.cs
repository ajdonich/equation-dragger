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

    public static AbstractTerm CreateTerm(string variable, int value, int denominator=1, bool dropSign=false) {
        VariableTerm varTerm = new VariableTerm(variable, value, denominator);
        varTerm.instantiate(dropSign);
        return varTerm;
    }

    public static GameObject instantiate(string text, bool clickable=true) {
        GameObject termObj = new GameObject(text);
        Image background = termObj.AddComponent<Image>();
        background.color = Color.clear;

        Rigidbody2D rbComp = termObj.AddComponent<Rigidbody2D>();
        rbComp.bodyType = RigidbodyType2D.Kinematic;

        // Finish init of collider below after text size calculated
        BoxCollider2D collComp = termObj.AddComponent<BoxCollider2D>();
        collComp.isTrigger = clickable; 

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
        textComp.fontStyle = FontStyle.Bold;
        textComp.fontSize = 25;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.black;
        textComp.text = text;

        // Finalize collider from calculated text size
        TextGenerator generator = new TextGenerator();            
        TextGenerationSettings settings = textComp.GetGenerationSettings(new Vector2(500.0f, 500.0f));
        collComp.size = new Vector2(generator.GetPreferredWidth(textComp.text, settings), 
                                    generator.GetPreferredHeight(textComp.text, settings));

        if (clickable) termObj.AddComponent<MouseHandler>();        
        return termObj;
    }
}

public abstract class AbstractTerm 
{
    public List<GameObject> gameObjects = new List<GameObject>();
    public List<Vector3> positions = new List<Vector3>();

    public abstract void instantiate(bool dropSign);
    public abstract int getValue();

    public void setParent(Transform parent) {
        foreach (GameObject go in gameObjects)
            go.transform.SetParent(parent);
    }

    public void setTextAlpha(float alpha) {
        foreach (GameObject go in gameObjects)
            go.transform.Find("Text").GetComponent<Text>().
                color = new Color(0.0f, 0.0f, 0.0f, alpha);
    }

    public void renderTerm(bool enabled) {
        foreach (GameObject go in gameObjects)
            go.transform.Find("Text").GetComponent<Text>().text = enabled ? go.name : "";
    }
}

public class ConstantTerm : AbstractTerm
{
    public int numerator;
    public int denominator;

    public ConstantTerm(int numerator, int denominator=1) {
        this.numerator = numerator;
        this.denominator = denominator;
    }

    public override void instantiate(bool dropSign=false) {
        string sign = numerator >= 0 ? " + " : " - ";
        if (dropSign) sign = numerator < 0 ? "- " : "";
        string text = sign + System.Math.Abs(numerator).ToString();
        gameObjects.Add(TermFactory.instantiate(text));
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

    public VariableTerm(string variable, int numerator, int denominator=1) {
        this.numerator = numerator;
        this.denominator = denominator;
        this.variable = variable;
    }

    public override void instantiate(bool dropSign=false) {
        string sign = numerator >= 0 ? " + " : " - ";
        if (dropSign) sign = numerator < 0 ? "- " : " ";
        
        int value = System.Math.Abs(numerator);
        if (value == 1) gameObjects.Add(TermFactory.instantiate(sign));
        else gameObjects.Add(TermFactory.instantiate(sign + value.ToString()));
        gameObjects.Add(TermFactory.instantiate(variable));
    }

    public override int getValue() {
        return numerator;
    }
}

