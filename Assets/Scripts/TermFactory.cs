using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermFactory
{
    public static Text getTextComp(GameObject termGo) {
        return termGo.transform.Find("Text").GetComponent<Text>();
    }

    private static TextGenerator generator = new TextGenerator();
    public static Vector2 getPreferredSize(Text textComp) {
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
    public bool hideSign;
    public bool hideOne;
    public Fraction rational;
    public List<VisualElement> visualElements;

    public abstract Vector2 getBounds();

    public VisualElement this[int index] {
        get => visualElements[index];
    }

    public void setTextAlpha(float alpha) {
        for (int i=0; i<visualElements.Count; ++i) 
            visualElements[i].setTextAlpha(alpha);
    }

    public void setBgColor(Color col) {
        for (int i=0; i<visualElements.Count; ++i) 
            visualElements[i].setBgColor(col);
    }

    public void update(Fraction rational, bool hideSign, bool hideOne) {
        if(this.rational != rational || this.hideSign != hideSign || this.hideOne != hideOne) {
            _fracVisElem().update(rational, hideSign, hideOne);
            this.rational = rational;
            this.hideSign = hideSign;
            this.hideOne = hideOne;
        }
    }

    public void setHideSign(bool hideSign) {
        if(this.hideSign != hideSign) {
            _fracVisElem().update(rational, hideSign, hideOne);
            this.hideSign = hideSign;
        }
    }

    public void setHideOne(bool hideOne) {
        if(this.hideOne != hideOne) {
            _fracVisElem().update(rational, hideSign, hideOne);
            this.hideOne = hideOne;
        }
    }

    public void setValue(Fraction rational) {
        if (this.rational != rational) {
            _fracVisElem().update(rational, hideSign, hideOne);
            this.rational = rational;
        }
    }

    public Fraction getValue() {
        return rational;
    }

    protected FracVisElem _fracVisElem() {
        return (FracVisElem)visualElements[0];
    }
}

public class ConstantTerm : AbstractTerm
{
    public ConstantTerm(GameObject parent, Fraction rational, bool hideSign) {
        visualElements = new List<VisualElement>();
        visualElements.Add(new FracVisElem(parent, rational, hideSign, false));
        this.rational = rational;
        this.hideSign = hideSign;
        this.hideOne = false;
    }

    public override Vector2 getBounds() {
        return visualElements[0].bounds;
    }
}

public class VariableTerm : AbstractTerm
{
    public VariableTerm(GameObject parent, Fraction rational, string variable, bool hideSign) {
        visualElements = new List<VisualElement>();
        visualElements.Add(new FracVisElem(parent, rational, hideSign, true));
        visualElements.Add(new VarVisElem(parent, variable));
        this.rational = rational;
        this.hideSign = hideSign;
        this.hideOne = true;
    }

    public Vector3 varOffset {
        get => Vector3.right * (visualElements[0].bounds.x/2.0f + visualElements[1].bounds.x/2.0f);
    }

    // Note: clipping height to the size of variable, not fraction,
    // since height just used to evaluate TermSeparated trigger box
    public override Vector2 getBounds() {
        Vector2 szcoe = visualElements[0].bounds;
        Vector2 szvar = visualElements[1].bounds;
        return new Vector2(szcoe.x + szvar.x, szvar.y);
    }
}
