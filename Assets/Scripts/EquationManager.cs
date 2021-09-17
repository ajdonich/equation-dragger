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
        public int initialValue;
        public Vector3 position;
        public AbstractTerm at;
        public GameObject go;

        public TermGoState(AbstractTerm at_a, GameObject go_a, Vector3 position_a) {
            initialValue = at_a.getValue();
            position = position_a;
            at = at_a;
            go = go_a;
        }

        public TermGoState(AbstractTerm at_a, GameObject go_a) 
            : this(at_a, go_a, go_a.transform.position) {}

        public TermGoState(TermGoState tgs) {
            initialValue = tgs.initialValue;
            position = tgs.position; 
            at = tgs.at;
            go = tgs.go;
        }

        public void updateValue(int value) {
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
    
    public enum SideType { 
        LHS = 1, 
        RHS = 2
    }

    public enum ClickedType { 
        Constant = 1, 
        Coefficient = 2, 
        Variable = 4 
    }

    public enum MoveState {
        InitialTouch = 1,
        TouchAborted = 2,
        TermSeparated = 3,
        DropAtCursor = 4,
        ConstOverConst = 5,
        DropAtConst = 6
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

    // Only searches currently active GameObjects
    public AbstractTerm GetTerm(GameObject go) {
        TermGoState tgs = history.Peek().Find(tgs => tgs.go == go);
        return tgs != null ? tgs.at : null;
    }

    private Vector3 GetHistPos(GameObject go) {
        TermGoState tgs = history.Peek().Find(tgs => tgs.go == go);
        return tgs != null ? tgs.position : Vector3.zero;
    }

    private int NumLftTerms(List<TermGoState> tgStates=null) {
        if (tgStates == null) tgStates = history.Peek();
        for (int i=0, nLftTerms=0; i<tgStates.Count; ++i) {
            if (tgStates[i].position.x > 0) return nLftTerms;
            else if (nLftTerms == 0 || tgStates[i-1].at != tgStates[i].at)
                nLftTerms += 1;
        }
        
        // Should never get here
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
            int iValue = Random.Range(-20, 20); // Value of coefficient/constant
            while (iValue == 0) iValue = Random.Range(-20, 20); // No 0 terms

            AbstractTerm at = null;
            if (Random.value < prob) {
                at = TermFactory.CreateTerm("x", iValue, 1, dropSign);
                nVarTerms -= 1;
            }
            else at = TermFactory.CreateTerm(iValue, 1, dropSign);
            at.setParent(equation.transform);

            for (int j=0; j<at.gameObjects.Count; ++j)            
                history.Peek().Add(new TermGoState(at, at.gameObjects[j]));
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
                tgStates[i].at.setDropSign(tnumb == 0 || tnumb == nLftTerms);
                terms.Add(tgStates[i].at); ++tnumb;
            }

            // Set/update collider sizes for all text comps
            tgStates[i].go.GetComponent<BoxCollider2D>().size = TermFactory.
                getPreferredSize(TermFactory.getTextComp(tgStates[i].go));
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
            for (int j=terms[i].gameObjects.Count-1; j>=0; --j) {
                TermGoState tgs = history.Peek().Find(tgs => tgs.go == terms[i].gameObjects[j]);
                float w = terms[i].gameObjects[j].GetComponent<BoxCollider2D>().size.x;
                tgs.position = Vector3.left * (lwidth + w/2.0f); lwidth += w; 
                cursorSlots[i] = Vector3.left * lwidth;
            }
        }

        // RHS (positive x-axis)
        for (int i=nLftTerms; i<terms.Count; ++i) {
            for (int j=0; j<terms[i].gameObjects.Count; ++j) {
                TermGoState tgs = history.Peek().Find(tgs => tgs.go == terms[i].gameObjects[j]);
                float w = terms[i].gameObjects[j].GetComponent<BoxCollider2D>().size.x;
                tgs.position = Vector3.right * (rwidth + w/2.0f); rwidth += w; 
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
            initPos[i] = tgStates[i].go.transform.position;

        float elapsed = 0;
        while (history.Count > 1 && elapsed < duration) 
        {
            for (int i=0; i<tgStates.Count; ++i) {
                tgStates[i].go.transform.position = Vector3.Lerp(
                    initPos[i], tgStates[i].position, elapsed/duration);

                if (tgStates[i].initialValue != tgStates[i].at.getValue())
                    tgStates[i].at.setText((int)Mathf.Round(Mathf.Lerp((float)tgStates[i].
                        initialValue, (float)tgStates[i].at.getValue(), elapsed/duration)));
            }

            for (int i=0; i<disolves.Count; ++i)
                disolves[i].at.setTextAlpha(Mathf.Lerp(1.0f, 0.0f, elapsed/duration));

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Assure final "snap" into place
        for (int i=0; i<tgStates.Count; ++i) {
            tgStates[i].go.transform.position = tgStates[i].position;
            if (tgStates[i].initialValue != tgStates[i].at.getValue()) {
                tgStates[i].at.setText(tgStates[i].at.getValue());
                tgStates[i].updateValue(tgStates[i].at.getValue());                
            }
        }
        
        for (int i=disolves.Count-1; i>=0; --i) {
            disolves[i].go.SetActive(false);
            disolves.RemoveAt(i);
        }

        animate_coroutine = null;
        yield return null;
    }

    // GameObjects instantiated in Start() (equation, cursor, equalSign) never
    // need to be destroyed, and equation never even needs to be deactivated.
    // Note: GameObject operator !=(null) is overloaded to detect Destroyed.
    private void ClearEquation() {
        while (history != null && history.Count > 0) {
            foreach (TermGoState tgs in history.Pop())
                if (tgs.go != null) Destroy(tgs.go); 
        }

        if (disolves != null)
            foreach (TermGoState tgs in disolves)
                if (tgs.go != null) Destroy(tgs.go); 

        equalSign.SetActive(false);
        cursor.SetActive(false);
    }

    ///////////////////////////////////////////////////////////////////////////
    // Mouse dragging/interaction handling
    ///////////////////////////////////////////////////////////////////////////

    class Mover
    {
        public SideType sdType;
        public ClickedType clkType;
        public TermGoState mvTgs;
        public MoveState mvState;
        public Vector3 offset;

        public Mover(ClickedType type, SideType side, TermGoState tgs, Vector3 mousePosition) {
            clkType = type; sdType = side; mvTgs = tgs; mvState = MoveState.InitialTouch;
            offset = Camera.main.WorldToScreenPoint(clickedGo().transform.position) - mousePosition;
        }

        public GameObject clickedGo() {
            return mvTgs.at.gameObjects[clkType == ClickedType.Variable ? 1 : 0];
        }

        public GameObject this[int index] {
            get => mvTgs.at.gameObjects[index];
        }

        public int getSignedValue() {
            int sdmult = (mvTgs.position.x * mvTgs.go.transform.position.x < 0) ? -1 : 1;
            return sdmult * mvTgs.at.getValue();
        }

        public void setSignedText() {
            int signedvalue = getSignedValue();
            TermFactory.getTextComp(mvTgs.go).text = 
                (signedvalue >= 0 ? " + " : " - ") + 
                System.Math.Abs(signedvalue).ToString();
        }

        public float getTextAlpha() {
            return TermFactory.getTextComp(mvTgs.go).color.a;
        }
    }

    private Mover mover;
    private AbstractTerm overlapped;

    private Vector3 GetCursorPos() {
        if (mover == null) return Vector3.zero;

        int minIdx = 0;
        float minDist = System.Math.Abs(cursorSlots[0].x - mover[0].transform.position.x);
        for (int i=1; i<cursorSlots.Length; ++i) {
            float dist = System.Math.Abs(cursorSlots[i].x - mover[0].transform.position.x);
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
                if (tgStates[i].position.x < 0) {
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
                List<TermGoState> tgStates = history.Peek();
                List<TermGoState> dragStates = new List<TermGoState>();
                for (int i=0; i<tgStates.Count; ++i) {
                    if (mover.mvTgs.at == tgStates[i].at) continue;
                    dragStates.Add(tgStates[i]);
                }

                // Replace mover term with zero term if need be
                if (dragStates[0].position.x > 0 || dragStates[dragStates.Count-1].position.x < 0) {
                    AbstractTerm zt = TermFactory.CreateZeroTerm();
                    zt.setParent(equation.transform);
                    dragStates.Insert(dragStates[0].position.x > 0 ? 0 : dragStates.Count, 
                        new TermGoState(zt, zt.gameObjects[0], mover.mvTgs.position));                    
                }

                mover.setSignedText();
                mover.mvState = MoveState.TermSeparated;
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
        case MoveState.TermSeparated:
        {
            mover.setSignedText();
            cursor.transform.position = GetCursorPos();
            if (GetOverlappedTerm(mover) is ConstantTerm)
                mover.mvState = MoveState.ConstOverConst;
            break;
        }
        case MoveState.DropAtCursor:
        {
            // Generate next equation state (if different)
            List<TermGoState> dragStates = history.Pop();
            mover.mvTgs.updateValue(mover.getSignedValue());
            mover.mvTgs.position = cursor.transform.position;
            for (int i=0; i<dragStates.Count+1; ++i) {
                if (i == dragStates.Count || mover.mvTgs.position.x < dragStates[i].position.x) {
                    dragStates.Insert(i, mover.mvTgs); 
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
        case MoveState.ConstOverConst:
        {
            AbstractTerm olap = GetOverlappedTerm(mover);
            if (olap != overlapped) {
                if (overlapped != null) {
                    overlapped.setValue(overlapped.getValue() - mover.getSignedValue());
                    overlapped.setBgColor(Color.clear);
                }

                if (olap is ConstantTerm) {
                    olap.setValue(olap.getValue() + mover.getSignedValue());
                    olap.setBgColor(MouseHandler.highlight);
                    cursor.SetActive(false);
                    overlapped = olap;
                }
                else {
                    mover.mvState = MoveState.TermSeparated;
                    cursor.SetActive(true);
                    overlapped = null;
                }
                LayoutTerms();
            }
            break;
        }
        case MoveState.DropAtConst:
        {
            if (overlapped != null) {
                TermGoState tgs = history.Peek().Find(
                    tgs => tgs.go == overlapped.gameObjects[0]);
                tgs.updateValue(overlapped.getValue());
                overlapped.setBgColor(Color.clear);

                // Remove unnecessary terms
                CleanZeroTerms(history.Peek());
                disolves.Add(mover.mvTgs);

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
            BoxCollider2D mvColl = mv.clickedGo().GetComponent<BoxCollider2D>();
            for (int i=0; i<mvColl.OverlapCollider(nonfilter, results); ++i)
                if(results[i].gameObject == equation) return true;
        }
        return false;
    }

    private AbstractTerm GetOverlappedTerm(Mover mv) {
        if (mv == null) return null;
              
        float maxArea = 0.0f;
        AbstractTerm maxOverlapped = null;
        Collider2D[] results = new Collider2D[5];
        ContactFilter2D nonfilter = new ContactFilter2D().NoFilter();
        BoxCollider2D mvColl = mv.clickedGo().GetComponent<BoxCollider2D>();
        RectTransform mvRect = mv.clickedGo().GetComponent<RectTransform>();

        for (int i=0; i<mvColl.OverlapCollider(nonfilter, results); ++i) {
            if(results[i].gameObject == equation) continue;

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
                maxOverlapped = GetTerm(results[i].gameObject);
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
                output += $" {tgStates[i].at.getValue()}";
            }
        }
        Debug.Log(output);
    }

    public void EqPartMouseDown(GameObject termGo, Vector3 mousePosition) {

        TermGoState tgs = history.Peek().Find(tgs => tgs.go == termGo);
        SideType side = tgs.position.x < 0 ? SideType.LHS : SideType.RHS;
        AbstractTerm term = GetTerm(termGo);

        // Put equation collider around term's current root position
        (Vector2 size, Vector2 offset) b = tgs.at.getBounds(GetHistPos(termGo));
        BoxCollider2D bcComp = equation.GetComponent<BoxCollider2D>();
        bcComp.size = b.size; bcComp.offset = b.offset;

        // Initialize mover
        if (tgs.at is ConstantTerm) 
            mover = new Mover(ClickedType.Constant, side, tgs, mousePosition);
        else if (termGo == tgs.at.gameObjects[0])
            mover = new Mover(ClickedType.Coefficient, side, tgs, mousePosition);
        else 
            mover = new Mover(ClickedType.Variable, side, tgs, mousePosition);        
    }

    public void EqPartMouseDrag(GameObject termGo, Vector3 mousePosition) {
        if (mover == null ) return;

        switch (mover.clkType)
        {
        case ClickedType.Constant:
            mover.mvTgs.go.transform.position = Camera.main.
                ScreenToWorldPoint(mousePosition + mover.offset);
            break;

        case ClickedType.Coefficient:
            mover.mvTgs.go.transform.position = 
                Camera.main.ScreenToWorldPoint(mousePosition + mover.offset);

            // TODO: do this right, like in layout (can't keep setting offset in drag function)
            // Vector3 offset2 = mover.mvTerm.rootPositions[1] - mover.mvTerm.rootPositions[0];
            // mover.mvTerm.gameObjects[1].transform.position = Camera.main.
            //     ScreenToWorldPoint(mousePosition + mover.offset + offset2);
            break;

        case ClickedType.Variable:
            mover.mvTgs.go.transform.position = Camera.main.
                ScreenToWorldPoint(mousePosition + mover.offset);
            break;

        default:
            Debug.Log($"Unrecognized ClickedType: {mover.clkType}.");
            break;
        }
    }

    public void EqPartMouseUp(GameObject termGo, Vector3 mousePosition) {
        if (mover == null) return;
        else if (mover.mvState == MoveState.InitialTouch)
            mover.mvState = MoveState.TouchAborted;
        else if (mover.mvState == MoveState.TermSeparated)
            mover.mvState = MoveState.DropAtCursor;
        else if (mover.mvState == MoveState.ConstOverConst)
            mover.mvState = MoveState.DropAtConst;
    }
}
