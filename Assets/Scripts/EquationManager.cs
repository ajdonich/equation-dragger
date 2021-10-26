using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquationManager : MonoBehaviour
{
    ///////////////////////////////////////////////////////////////////////////
    // Initial equation instantiation and layout
    ///////////////////////////////////////////////////////////////////////////

    public class TermGoState {
        public Fraction initialValue;
        public Vector3 tsposition; // typeset position
        public AbstractTerm at;
        public VisualElement ve;

        public TermGoState(AbstractTerm at_a, VisualElement ve_a, Vector3 position_a) {
            initialValue = at_a.getValue();
            tsposition = position_a;
            at = at_a;
            ve = ve_a;
        }

        public TermGoState(AbstractTerm at_a, VisualElement ve_a) 
            : this(at_a, ve_a, ve_a.rootPos) {}

        public TermGoState(TermGoState tgs) {
            initialValue = tgs.initialValue;
            tsposition = tgs.tsposition; 
            at = tgs.at;
            ve = tgs.ve;
        }

        public void updateValue(Fraction value) {
            at.setValue(value);
            initialValue = value;
        }
    }

    private GameObject equation;
    private GameObject equalSign;
    private GameObject cursor;
    private Vector3[] cursorSlots;
    private Stack<List<TermGoState>> history;
    private List<TermGoState> disolves;
    
    public enum ClickedType { 
        Constant = 1, 
        Coefficient = 2, 
        Variable = 4 
    }

    public enum MoveState {
        InitialTouch = 1,
        TouchAborted = 2,
        AddTermSeparated = 3,
        MultTermSeparated = 4,
        DropAtCursor = 5,
        TermOverSum = 6,
        DropAtSum = 7
    }

    void Start() {
        // Equation is just an empty to parent terms under, with
        // a utility collider for detecting term movement bounds
        equation = new GameObject("Equation");
        equation.transform.SetParent(transform.parent);
        equation.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        equation.AddComponent<BoxCollider2D>().isTrigger = true;

        equalSign = TermFactory.instantiate(" = ", false);
        equalSign.transform.SetParent(equation.transform);
        equalSign.SetActive(false);

        cursor = TermFactory.instantiate("|", false, FontStyle.Normal);
        TermFactory.getTextComp(cursor.gameObject).color = CursorBlink.onColor;
        cursor.transform.SetParent(equation.transform);
        cursor.AddComponent<CursorBlink>();
        cursor.SetActive(false);
    }

    private Vector3 GetHistPos(GameObject go) {
        TermGoState tgs = history.Peek().Find(tgs => tgs.ve.contains(go));
        return tgs != null ? tgs.tsposition : Vector3.zero;
    }

    private int NumLftTerms(List<TermGoState> tgStates=null) {
        if (tgStates == null) tgStates = history.Peek();
        for (int i=0, nLftTerms=0; i<tgStates.Count; ++i) {
            if (tgStates[i].tsposition.x > 0) return nLftTerms;
            else if (nLftTerms == 0 || tgStates[i-1].at != tgStates[i].at)
                nLftTerms += 1;
        }
        
        // Require at least one term per side
        Debug.Assert(false); return 0; 
    }

    public void SpawnEquation() 
    {
        ClearEquation();
        equalSign.SetActive(true);
        history = new Stack<List<TermGoState>>();
        disolves = new List<TermGoState>();

        int nLftTerms = Random.Range(1, 5);
        int nTerms = nLftTerms + Random.Range(nLftTerms > 1 ? 1 : 2, 5);
        int nVarTerms = Random.Range(1, nTerms);

        // Instantiate series of random terms
        history.Push(new List<TermGoState>());
        List<AbstractTerm> terms = new List<AbstractTerm>();
        for (int i=0; i<nTerms; ++i)
        {
            float prob = ((float)nVarTerms)/(nTerms-i); // Prob of variable term
            bool dropSign = (i == 0 || i == nLftTerms); // Drop if implicit (left-most)
            Fraction rational = Random.value < 0.5 
                ? new Fraction(Random.Range(-20, 20), Random.Range(1, 9))
                : new Fraction(Random.Range(-20, 20), 1);

            // No initial zero terms
            while (rational.num == 0) 
                rational = new Fraction(Random.Range(-20, 20), rational.den); 

            AbstractTerm at = null;
            if (Random.value < prob) {
                at = new VariableTerm(equation, rational, "x", dropSign);
                nVarTerms -= 1;
            }
            else at = new ConstantTerm(equation, rational, dropSign);

            // Initialize a TermGoState per VisualElement
            for (int j=0; j<at.visualElements.Count; ++j)
                history.Peek().Add(new TermGoState(at, at[j]));
        }

        // Typeset/position terms
        LayoutTerms(nLftTerms);
    }


    // Note: expects TermGoStates have been added to history
    private void LayoutTerms(int nLftTerms = -1)
    {
        if (nLftTerms < 0) nLftTerms = NumLftTerms();
        List<TermGoState> tgStates = history.Peek();
        List<AbstractTerm> terms = new List<AbstractTerm>();
        for (int i=0, tnumb=0; i<tgStates.Count; ++i) {
            if (terms.Count == 0 || terms[terms.Count-1] != tgStates[i].at) {
                tgStates[i].at.setHideSign(tnumb == 0 || tnumb == nLftTerms);
                terms.Add(tgStates[i].at); ++tnumb;
            }
        }

        // Equal sign always remains at center/origin
        float lwidth, rwidth;
        lwidth = rwidth = TermFactory.getPreferredSize(
            TermFactory.getTextComp(equalSign)).x/2.0f;

        // Two slots sandwich the equal sign
        cursorSlots = new Vector3[terms.Count+2];
        cursorSlots[nLftTerms] = Vector3.left * lwidth;
        cursorSlots[nLftTerms+1] = Vector3.right * rwidth;

        // LHS (negative x-axis)
        for (int i=nLftTerms-1; i>=0; --i) {
            for (int j=terms[i].visualElements.Count-1; j>=0; --j) {
                terms[i].visualElements[j].calculateBounds();
                Vector2 rect = terms[i].visualElements[j].bounds;
                TermGoState tgs = history.Peek().Find(tgs => tgs.ve == terms[i].visualElements[j]);

                if (tgs == null) Debug.Log(terms[i].visualElements[j].rootVisElemGo.name);

                tgs.tsposition = Vector3.left * (lwidth + rect.x/2.0f); lwidth += rect.x; 
                cursorSlots[i] = Vector3.left * lwidth;
            }
        }

        // RHS (positive x-axis)
        for (int i=nLftTerms; i<terms.Count; ++i) {
            for (int j=0; j<terms[i].visualElements.Count; ++j) {
                terms[i].visualElements[j].calculateBounds();
                Vector2 rect = terms[i].visualElements[j].bounds;
                TermGoState tgs = history.Peek().Find(tgs => tgs.ve == terms[i].visualElements[j]);

                if (tgs == null) Debug.Log(terms[i].visualElements[j].rootVisElemGo.name);

                tgs.tsposition = Vector3.right * (rwidth + rect.x/2.0f); rwidth += rect.x; 
                cursorSlots[i+2] = Vector3.right * rwidth;
            }
        }

        // Ensure only one animation coroutine running
        if (animate_coroutine != null) StopCoroutine(animate_coroutine);
        animate_coroutine = AnimateLayout();
        StartCoroutine(animate_coroutine);
    }
    
    private IEnumerator animate_coroutine = null;
    IEnumerator AnimateLayout(float duration=0.25f)
    {
        List<TermGoState> tgStates = history.Peek();
        Vector3[] initPos = new Vector3[tgStates.Count];
        for (int i=0; i<tgStates.Count; ++i)
            initPos[i] = tgStates[i].ve.rootPos;

        float elapsed = 0;
        while (history.Count > 1 && elapsed < duration) 
        {
            for (int i=0; i<tgStates.Count; ++i) {
                tgStates[i].ve.setPosition(Vector3.Lerp(initPos[i], 
                    tgStates[i].tsposition, elapsed/duration));

                // if (tgStates[i].initialValue != tgStates[i].at.getValue())
                //     tgStates[i].at.setText((int)Mathf.Round(Mathf.Lerp((float)tgStates[i].
                //         initialValue, (float)tgStates[i].at.getValue(), elapsed/duration)));
            }

            for (int i=0; i<disolves.Count; ++i)
                disolves[i].at.setTextAlpha(Mathf.Lerp(1.0f, 0.0f, elapsed/duration));

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Assure final "snap" into place
        for (int i=0; i<tgStates.Count; ++i) {
            tgStates[i].ve.setPosition(tgStates[i].tsposition);
            if (tgStates[i].initialValue != tgStates[i].at.getValue())
                tgStates[i].updateValue(tgStates[i].at.getValue());                
        }
        
        for (int i=disolves.Count-1; i>=0; --i) {
            disolves[i].ve.setActive(false);
            disolves.RemoveAt(i);
        }

        animate_coroutine = null;
        yield return null;
    }

    // GameObjects instantiated in Start() (equation, cursor, equalSign) never
    // need to be destroyed, and equation never even needs to be deactivated.
    private void ClearEquation() {
        while (history != null && history.Count > 0)
            foreach (TermGoState tgs in history.Pop()) tgs.ve.destroy();

        if (disolves != null)
            foreach (TermGoState tgs in disolves) tgs.ve.destroy();

        equalSign.SetActive(false);
        cursor.SetActive(false);
    }

    ///////////////////////////////////////////////////////////////////////////
    // Mouse dragging/interaction handling
    ///////////////////////////////////////////////////////////////////////////

    class Mover
    {
        public ClickedType clkType;
        public TermGoState mvTgs;
        public MoveState mvState;
        public GameObject clickedGo;
        public Vector3 offset;
        public TermGoState varTgs;
        public bool dragVarLock;

        public Mover(ClickedType type, TermGoState tgs, GameObject go, Vector3 mousePosition) {
            clkType = type; mvTgs = tgs; mvState = MoveState.InitialTouch; clickedGo = go;
            offset = Camera.main.WorldToScreenPoint(go.transform.position) - mousePosition;
            dragVarLock = false;
        }

        public Fraction getSignedValue(TermGoState olap=null) {
            float xfinal = (olap != null) ? olap.tsposition.x : mvTgs.ve.rootPos.x;
            return (mvTgs.tsposition.x * xfinal < 0) ? -mvTgs.initialValue : mvTgs.initialValue;
        }

        public void setSignedText(TermGoState olap=null) {
            Fraction signedValue = getSignedValue(olap);
            if (signedValue != mvTgs.at.getValue()) {
                mvTgs.at[0].calculateBounds();
                mvTgs.at.update(signedValue, false, mvTgs.at.hideOne);
            }
        }

        // For dropping mover at the cursor, this is 
        // approximate, LayoutTerms finalizes typeset
        public void updatePosition(Vector3 cursorPos) {
            mvTgs.tsposition = cursorPos;
            if (varTgs != null && additiveMove()) 
                varTgs.tsposition = cursorPos;
        }

        public bool additiveMove() {
            return dragVarLock || !(mvTgs.at is VariableTerm) || (mvTgs.ve.rootPos.y > 0); 
            // return dragVarLock || (mvTgs.at is VariableTerm && mvTgs.ve.rootPos.y > 0);
        }

        public void lockDragVariable() {
            dragVarLock = additiveMove();
        }
    }

    private Mover mover;
    private TermGoState overlapped;

    private Vector3 GetCursorPos() {
        if (mover == null) return Vector3.zero;

        int minIdx = 0;
        float minDist = System.Math.Abs(cursorSlots[0].x - mover.mvTgs.ve.rootPos.x);
        for (int i=1; i<cursorSlots.Length; ++i) {
            float dist = System.Math.Abs(cursorSlots[i].x - mover.mvTgs.ve.rootPos.x);
            if (dist < minDist) {
                minDist = dist;
                minIdx = i;
            }
        }

        return cursorSlots[minIdx];
    }

    private void CleanZeroTerms(List<TermGoState> tgStates)
    {
        int nlft=0, nrht=0;
        List<int> ztLftIdx = new List<int>();
        List<int> ztRhtIdx = new List<int>();

        // Count terms and zero terms
        AbstractTerm atPrev = null;
        for (int i=0; i<tgStates.Count; ++i) {
            if (atPrev != tgStates[i].at) {
                atPrev = tgStates[i].at; 
                if (tgStates[i].tsposition.x < 0) {
                    if (tgStates[i].at.getValue() == 0) ztLftIdx.Add(i);
                    nlft += 1;
                }
                else {
                    if (tgStates[i].at.getValue() == 0) ztRhtIdx.Add(i);
                    nrht += 1;
                }
            }
        }

        // Always need at least one term per side, even if zero
        if (ztLftIdx.Count == nlft) ztLftIdx.RemoveAt(ztLftIdx.Count-1);
        if (ztRhtIdx.Count == nrht) ztRhtIdx.RemoveAt(0);

        // Othersize remove all zero terms
        foreach (int i in ztLftIdx) { disolves.Add(tgStates[i]); tgStates.RemoveAt(i); }
        foreach (int i in ztRhtIdx) { disolves.Add(tgStates[i]); tgStates.RemoveAt(i); }
    }

    private void Update()  
    {
        if (mover == null) return;
        switch (mover.mvState)
        {
        case MoveState.InitialTouch:
        {
            if (!OverlapsEquation(mover)) 
            {
                // Generate intermediate (mid-drag) equation state
                bool addmv = mover.additiveMove();
                List<TermGoState> tgStates = history.Peek();
                List<TermGoState> dragStates = new List<TermGoState>();
                for (int i=0; i<tgStates.Count; ++i) {
                    if (mover.mvTgs.at != tgStates[i].at || (mover.mvTgs.ve != tgStates[i].ve && !addmv))
                        dragStates.Add(tgStates[i]);
                }

                // Replace mover term with zero term if need be
                if (dragStates[0].tsposition.x > 0 || dragStates[dragStates.Count-1].tsposition.x < 0) {                    
                    AbstractTerm zt = new ConstantTerm(equation, new Fraction(0,1), true);
                    dragStates.Insert(dragStates[0].tsposition.x > 0 ? 0 : dragStates.Count, 
                        new TermGoState(zt, zt[0], mover.mvTgs.tsposition));                    
                }

                mover.mvState = addmv
                    ? MoveState.AddTermSeparated
                    : MoveState.MultTermSeparated;
                cursor.SetActive(true);
                history.Push(dragStates);
                LayoutTerms();
            }
            break;
        }
        case MoveState.TouchAborted:
        {
            mover = null;
            LayoutTerms();
            break;
        }
        case MoveState.AddTermSeparated:
        {
            mover.setSignedText();
            mover.lockDragVariable();
            cursor.transform.position = GetCursorPos();
            TermGoState olap = GetOverlappedTgs(mover);
            if (olap != null && ((mover.mvTgs.at is ConstantTerm && olap.at is ConstantTerm) || 
                (mover.mvTgs.at is VariableTerm && olap.at is VariableTerm && mover.additiveMove())))
                mover.mvState = MoveState.TermOverSum;

            // if (GetOverlappedTerm(mover) is ConstantTerm)
            //     mover.mvState = MoveState.TermOverSum;
            break;
        }
        case MoveState.MultTermSeparated:
        {
            
            break;
        }
        case MoveState.DropAtCursor:
        {
            // Generate next equation state (if different)
            List<TermGoState> dragStates = history.Pop();
            mover.mvTgs.updateValue(mover.getSignedValue());
            mover.updatePosition(cursor.transform.position);
            for (int i=0; i<dragStates.Count+1; ++i) {
                if (i == dragStates.Count || mover.mvTgs.tsposition.x < dragStates[i].tsposition.x) {
                    dragStates.Insert(i, mover.mvTgs); 
                    if (mover.varTgs != null && mover.additiveMove())
                        dragStates.Insert(i+1, mover.varTgs); 
                    break;
                }
            }

            mover = null;
            cursor.SetActive(false);
            CleanZeroTerms(dragStates);
            history.Push(dragStates);
            LayoutTerms();
            break;
        }
        case MoveState.TermOverSum:
        {
            TermGoState olap = GetOverlappedTgs(mover);

            if (olap != overlapped) {
                if (overlapped != null) {
                    overlapped.at.setValue(overlapped.at.getValue() - mover.getSignedValue(overlapped));
                    overlapped.at.setBgColor(Color.clear);
                }

                if (olap != null && ((mover.mvTgs.at is ConstantTerm && olap.at is ConstantTerm) || 
                    (mover.mvTgs.at is VariableTerm && olap.at is VariableTerm && mover.additiveMove()))) {
                    olap.at.setValue(olap.at.getValue() + mover.getSignedValue(olap));
                    olap.at.setBgColor(MouseHandler.highlight);
                    cursor.SetActive(false);
                    overlapped = olap;
                }
                else {
                    mover.mvState = MoveState.AddTermSeparated;
                    cursor.SetActive(true);
                    overlapped = null;
                }
                LayoutTerms();
            }
            break;
        }
        case MoveState.DropAtSum:
        {
            if (overlapped != null) {
                overlapped.updateValue(overlapped.at.getValue());
                overlapped.at.setBgColor(Color.clear);

                // Remove unnecessary terms
                CleanZeroTerms(history.Peek());
                disolves.Add(mover.mvTgs);
                if (mover.varTgs != null && mover.additiveMove())
                    disolves.Add(mover.varTgs);

                mover = null;
                overlapped = null;
                cursor.SetActive(false);
                LayoutTerms();
            }
            else mover.mvState = MoveState.DropAtCursor;
            break;
        }
        default:
            Debug.Log("$Unrecognized MoveState: {mover.mvState}");
            break;
        }
    }

    private bool OverlapsEquation(Mover mv) {
        if (mv != null) {
            Collider2D[] results = new Collider2D[5];
            ContactFilter2D nonfilter = new ContactFilter2D().NoFilter();
            BoxCollider2D mvColl = mv.clickedGo.GetComponent<BoxCollider2D>();
            for (int i=0; i<mvColl.OverlapCollider(nonfilter, results); ++i)
                if(results[i].gameObject == equation) return true;
        }
        return false;
    }

    private TermGoState GetOverlappedTgs(Mover mv) {
        if (mv == null) return null;

        float maxArea = 0.0f;
        TermGoState maxOverlapped = null;
        Collider2D[] results = new Collider2D[5];
        ContactFilter2D nonfilter = new ContactFilter2D().NoFilter();
        BoxCollider2D mvColl = mv.clickedGo.GetComponent<BoxCollider2D>();
        RectTransform mvRect = mv.clickedGo.GetComponent<RectTransform>();

        for (int i=0; i<mvColl.OverlapCollider(nonfilter, results); ++i) {
            if(results[i].gameObject == equation) continue;

            TermGoState tgs = history.Peek().Find(tgs => tgs.ve.contains(results[i].gameObject));
            if (tgs == null || (mv.mvTgs.at is ConstantTerm && !(tgs.at is ConstantTerm))) continue;

            RectTransform udRect = results[i].gameObject.GetComponent<RectTransform>();
            float mv_xMin = mvRect.position.x - mvRect.rect.width/2;
            float ud_xMin = udRect.position.x - udRect.rect.width/2;
            float mv_xMax = mvRect.position.x + mvRect.rect.width/2;
            float ud_xMax = udRect.position.x + udRect.rect.width/2;
            float mv_yMin = mvRect.position.y - mvRect.rect.height/2;
            float ud_yMin = udRect.position.y - udRect.rect.height/2;
            float mv_yMax = mvRect.position.y + mvRect.rect.height/2;
            float ud_yMax = udRect.position.y + udRect.rect.height/2;

            float xlen = Mathf.Min(mv_xMax, ud_xMax) - Mathf.Max(mv_xMin, ud_xMin);
            float ylen = Mathf.Min(mv_yMax, ud_yMax) - Mathf.Max(mv_yMin, ud_yMin);
            float area = xlen * ylen;

            if (area > maxArea) {
                maxOverlapped = tgs;
                maxArea = area;
            }
        }

        return maxOverlapped;
    }

    private void debugPrintEqValues() {
        string output = "";
        List<TermGoState> tgStates = history.Peek();
        for (int i=0; i<tgStates.Count; ++i) {
            if (i == 0 || tgStates[i-1].at != tgStates[i].at) {
                output += $"({tgStates[i].tsposition.x}) {tgStates[i].at.getValue()}, ";
            }
        }
        Debug.Log(output);
    }

    public void EqPartMouseDown(GameObject termGo, Vector3 mousePosition) {

        List<TermGoState> tgStates = history.Peek();
        int idx = tgStates.FindIndex(tgs => tgs.ve.contains(termGo));
        TermGoState tgs = tgStates[idx];
        Cursor.visible = false;

        // Put equation collider around term's current root position
        BoxCollider2D bcComp = equation.GetComponent<BoxCollider2D>();
        bcComp.offset = new Vector2(tgStates[idx].ve.rootPos.x, tgStates[idx].ve.rootPos.y);
        bcComp.size = tgStates[idx].ve.bounds;

        // Initialize mover
        if (tgStates[idx].at is ConstantTerm)
            mover = new Mover(ClickedType.Constant, tgStates[idx], termGo, mousePosition);
        else if (tgStates[idx].ve is FracVisElem) {
            mover = new Mover(ClickedType.Coefficient, tgStates[idx], termGo, mousePosition);
            mover.varTgs = tgStates[idx+1];
        }
        else
            mover = new Mover(ClickedType.Variable, tgStates[idx], termGo, mousePosition);
    }

    public void EqPartMouseDrag(GameObject termGo, Vector3 mousePosition) {
        if (mover == null ) return;
        Vector3 mvScreenPos = mousePosition + mover.offset;

        switch (mover.clkType)
        {
        case ClickedType.Constant:
            mover.mvTgs.ve.setPosition(Camera.main.ScreenToWorldPoint(mvScreenPos));
            break;

        case ClickedType.Coefficient:
            mover.mvTgs.ve.setPosition(Camera.main.ScreenToWorldPoint(mvScreenPos));
            if (mover.additiveMove()) {
                VariableTerm vt = (VariableTerm)mover.mvTgs.at;
                vt[1].setPosition(Camera.main.ScreenToWorldPoint(mvScreenPos + vt.varOffset));
            }
            break;

        case ClickedType.Variable:
            mover.mvTgs.ve.setPosition(Camera.main.ScreenToWorldPoint(mvScreenPos));
            break;

        default:
            Debug.Log($"Unrecognized ClickedType: {mover.clkType}.");
            break;
        }
    }

    public void EqPartMouseUp(GameObject termGo, Vector3 mousePosition) {
        Cursor.visible = true;
        if (mover == null) return;
        else if (mover.mvState == MoveState.InitialTouch)
            mover.mvState = MoveState.TouchAborted;
        else if (mover.mvState == MoveState.AddTermSeparated)
            mover.mvState = MoveState.DropAtCursor;
        else if (mover.mvState == MoveState.TermOverSum)
            mover.mvState = MoveState.DropAtSum;
    }
}
