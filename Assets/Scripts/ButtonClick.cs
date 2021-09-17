using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour
{
    public GameObject RefToText;
    private Vector3 vectorCopy;

    Vector3 offset;
    void OnMouseDown() {
        offset = Camera.main.WorldToScreenPoint(RefToText.transform.position) - Input.mousePosition;
    }

    void OnMouseDrag() {
        RefToText.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition + offset);
    }

    void Start()  {
        vectorCopy = RefToText.transform.position;
    }

    public void printPosition() {
        Debug.Log($"Actual position: {RefToText.transform.position}, vector: {vectorCopy}");
    }

    // public GameObject equationPrefab;
    // private GameObject equation = null;
    // private IEnumerator coroutine = null;
    // private float resizeTimer = 3.0f;

    // public void SpawnEquation() 
    // {        
    //     int nLftTerms = Random.Range(1, 5);        
    //     int nTerms = nLftTerms + Random.Range(nLftTerms > 1 ? 1 : 2, 5);
    //     int nVarTerms = Random.Range(1, nTerms);
        
    //     StringBuilder eq = new StringBuilder();
    //     for (int i=0; i<nTerms; i++) 
    //     {    
    //         // Coefficient/constant value
    //         int cValue = Random.Range(1, 20);

    //         // Probablity of variable (vs constant) term 
    //         float prob = ((float)nVarTerms)/(nTerms-i);
    //         if (Random.value >= prob)
    //             eq.Append(cValue.ToString());
    //         else {
    //             eq.Append(cValue.ToString() + "x");
    //             nVarTerms -= 1;
    //         }

    //         if (i == nLftTerms-1) 
    //             eq.Append(" = ");
    //         else if (i<nTerms-1) 
    //             eq.Append(Random.value < 0.5f ? " + " : " - ");
    //     }

    //     if (equation) {
    //         StopCoroutine(coroutine);
    //         Destroy(equation);
    //     }

    //     equation = (GameObject)Instantiate(equationPrefab, Vector3.zero, Quaternion.identity);
    //     equation.transform.SetParent(transform.parent);
    //     equation.name = "Equation";

    //     RectTransform rectComp = equation.GetComponent<RectTransform>();
    //     rectComp.localScale = Vector3.one;

    //     Text textComp = equation.GetComponent<Text>();
    //     textComp.text = eq.ToString();
        
    //     resizeTimer = 3.0f;
    //     coroutine = SetColliderSize(textComp);
    //     StartCoroutine(coroutine);

    //     // Debug.Log("preferredHeight: " + textComp.preferredHeight + 
    //     //           ", preferredWidth: " + textComp.preferredWidth);
    // }

    // IEnumerator SetColliderSize(Text textComp)
    // {
    //     /*  Must wait for Unity Layout and Content Fitter to finalize
    //         assigning Text and Rect Transform sizes before we can set 
    //         BoxCollider size. Because OnRectTransformDimensionsChange
    //         does not seem to be reliable triggered, using this coroutine.
    //         Waits a maximum of resizeTimer seconds for size to change. */

    //     float height = textComp.preferredHeight;
    //     float width = textComp.preferredWidth;

    //     bool sizeChanged = false;
    //     while (resizeTimer > 0.0f && !sizeChanged) {
    //         sizeChanged = (textComp.preferredHeight != height || textComp.preferredWidth != width);
    //         resizeTimer -= Time.deltaTime;
    //         yield return null;
    //     }

    //     BoxCollider2D colliderComp = equation.GetComponent<BoxCollider2D>();
    //     colliderComp.size = new Vector2(textComp.preferredWidth, textComp.preferredHeight);
    //     yield return null;
    // }
}
