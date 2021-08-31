using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class MouseHandler : MonoBehaviour, IPointerEnterHandler, 
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static Color highlight = new Color(1.0f, 1.0f, 1.0f, 0.1f);
    private EquationManager equationManager;
    private bool leftDown = false;

    private void Start() {
        equationManager = GameObject.Find("EquationButton").GetComponent<EquationManager>();
    }

    public void OnPointerDown(PointerEventData eventData) {
        leftDown = eventData.button == PointerEventData.InputButton.Left;
        if (leftDown) equationManager.EqPartMouseDown(gameObject, eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (leftDown) {
            equationManager.EqPartMouseUp(gameObject, eventData.position);
            leftDown = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        gameObject.GetComponent<UnityEngine.UI.Image>().color = MouseHandler.highlight;

        AbstractTerm term = equationManager.termMap[gameObject];
        if (term is VariableTerm && gameObject == term.gameObjects[0])
            term.gameObjects[1].GetComponent<UnityEngine.UI.Image>().color = MouseHandler.highlight;
    }

    public void OnPointerExit(PointerEventData eventData) {
        AbstractTerm term = equationManager.termMap[gameObject];
        foreach (GameObject go in term.gameObjects) 
            go.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
    }

    public void OnDrag(PointerEventData eventData) {
        if (leftDown) equationManager.EqPartMouseDrag(gameObject, eventData.position);
    }
}
