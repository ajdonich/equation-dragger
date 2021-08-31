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

    private GameObject equation;
    private GameObject equalSign;
    private List<AbstractTerm> terms = new List<AbstractTerm>();
    public Dictionary<GameObject, AbstractTerm> termMap = 
        new Dictionary<GameObject, AbstractTerm>();

    public void SpawnEquation() 
    {
        ClearEquation();
        equation = new GameObject("Equation"); // Empty
        equation.transform.SetParent(transform.parent);
        
        equalSign = TermFactory.instantiate(" = ", false);
        equalSign.transform.SetParent(equation.transform);

        int nLftTerms = Random.Range(1, 5);        
        int nTerms = nLftTerms + Random.Range(nLftTerms > 1 ? 1 : 2, 5);
        int nVarTerms = Random.Range(1, nTerms);
        
        // Instantiate at origin
        for (int i=0; i<nTerms; i++) 
        {
            float prob = ((float)nVarTerms)/(nTerms-i); // Prob of variable term
            bool dropSign = (i == 0 || i == nLftTerms); // Drop if implicit (left-most)
            int iValue = Random.Range(-20, 20); // Value of coefficient/constant
            while (iValue == 0) iValue = Random.Range(-20, 20); // No 0 terms

            if (Random.value < prob) {
                terms.Add(TermFactory.CreateTerm("x", iValue, 1, dropSign));
                nVarTerms -= 1;
            }
            else terms.Add(TermFactory.CreateTerm(iValue, 1, dropSign));
            terms[terms.Count-1].setParent(equation.transform);
        }

        // Layout positions
        float lwidth, rwidth;
        lwidth = rwidth = equalSign.GetComponent<BoxCollider2D>().size.x/2.0f;

        for (int i=nLftTerms-1; i>=0; i--) {
            for (int j=terms[i].gameObjects.Count-1; j>=0; j--) {
                float w = terms[i].gameObjects[j].GetComponent<BoxCollider2D>().size.x;
                terms[i].gameObjects[j].transform.position = Vector3.left * (lwidth + w/2.0f);
                terms[i].positions.Insert(0, Vector3.left * (lwidth + w/2.0f));
                lwidth += w;
            }
        }
        
        for (int i=nLftTerms; i<terms.Count; i++) {
            for (int j=0; j<terms[i].gameObjects.Count; j++) {
                float w = terms[i].gameObjects[j].GetComponent<BoxCollider2D>().size.x;
                terms[i].gameObjects[j].transform.position = Vector3.right * (rwidth + w/2.0f);
                terms[i].positions.Insert(j, Vector3.right * (rwidth + w/2.0f));
                rwidth += w;
            }
        }

        // Finalize map structure
        foreach(AbstractTerm t in terms)
            foreach (GameObject go in t.gameObjects) 
                termMap.Add(go, t);
    }

    private void ClearEquation()
    {
        foreach(KeyValuePair<GameObject, AbstractTerm> pair in termMap)
            Destroy(pair.Key);
        
        terms.Clear();
        termMap.Clear();

        if (equation) {
            Destroy(equation);
            equation = null;
        }

        if (equalSign) {
            Destroy(equalSign);
            equalSign = null;
        }
    }

    ///////////////////////////////////////////////////////////////////////////
    // Mouse dragging/interaction handling
    ///////////////////////////////////////////////////////////////////////////

    public enum SideType { 
        LHS = 1, 
        RHS = 2
    }

    public enum MoveType { 
        Constant = 1, 
        Coefficient = 2, 
        Variable = 4 
    }

    class Mover
    {
        public MoveType mvType;
        public SideType sdType;
        public AbstractTerm mvTerm;
        public Vector3 offset;

        public Mover(MoveType type, AbstractTerm term, Vector3 mousePosition) {
            mvTerm = term;
            mvType = type;
            sdType = mvTerm.positions[0].x < 0 ? SideType.LHS : SideType.RHS;
            offset = Camera.main.WorldToScreenPoint(clickedGo().transform.position) - mousePosition;
        }

        public GameObject clickedGo() {
            return mvTerm.gameObjects[mvType == MoveType.Variable ? 1 : 0];
        }
    }

    private Mover mover;
    private AbstractTerm overlapped;

    private void Update()  {
        if (mover == null) return;

        AbstractTerm olap = getOverlappedTerm(mover);
        if (olap != null && olap != overlapped) {
            if (overlapped != null)
                StartCoroutine(SeparateTerms(mover, overlapped));

            colorOverlap(olap);
            if (mover.mvTerm is ConstantTerm && overlapped is ConstantTerm) 
                StartCoroutine(CombineTerms(mover, overlapped));
        }
        else if (olap == null && overlapped != null) {
            StartCoroutine(SeparateTerms(mover, overlapped));
            colorOverlap();
        }
    }

    // Clears existing overlap, highlighs new overlap if passed
    private void colorOverlap(AbstractTerm newOlap=null) {
        if (overlapped != null) {
            foreach (GameObject go in overlapped.gameObjects)
                go.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        }

        overlapped = newOlap;
        if (overlapped != null) {
            foreach (GameObject go in overlapped.gameObjects)
                go.GetComponent<UnityEngine.UI.Image>().color = MouseHandler.highlight;
        }
    }


    private AbstractTerm getOverlappedTerm(Mover mv) {
        if (mv == null) return null;
              
        float maxArea = 0.0f;
        AbstractTerm maxOverlapped = null;
        Collider2D[] results = new Collider2D[5];
        ContactFilter2D nonfilter = new ContactFilter2D().NoFilter();
        BoxCollider2D mvColl = mv.clickedGo().GetComponent<BoxCollider2D>();
        RectTransform mvRect = mv.clickedGo().GetComponent<RectTransform>();

        for (int i=0; i<mvColl.OverlapCollider(nonfilter, results); i++) {
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
                maxOverlapped = termMap[results[i].gameObject];
                maxArea = area;
            }
        }

        return maxOverlapped;
    }

    // private getOverlappedTerm() {
    //     if (mover != null) {
    //         Collider2D[] results = new Collider2D[10];
    //         ContactFilter2D filter = new ContactFilter2D();
    //         HashSet<GameObject> doHighlight = new HashSet<GameObject>();

    //         foreach(KeyValuePair<GameObject, AbstractTerm> pair in termMap) {
    //             if (mover.mvTerm.gameObjects.Contains(pair.Key)) continue;
    //             RectTransform rt1 = pair.Key.GetComponent<RectTransform>();
    //             int n = pair.Key.GetComponent<BoxCollider2D>().OverlapCollider(
    //                 filter.NoFilter(), results);

    //             bool trueOverlap = false;
    //             for (int i=0; i<n; i++) {
    //                 RectTransform rt2 = results[i].gameObject.GetComponent<RectTransform>();

    //                 float r1_xMin = rt1.position.x - rt1.rect.width/2;
    //                 float r2_xMin = rt2.position.x - rt2.rect.width/2;
    //                 float r1_xMax = rt1.position.x + rt1.rect.width/2;
    //                 float r2_xMax = rt2.position.x + rt2.rect.width/2;

    //                 float r1_yMin = rt1.position.y - rt1.rect.height/2;
    //                 float r2_yMin = rt2.position.y - rt2.rect.height/2;
    //                 float r1_yMax = rt1.position.y + rt1.rect.height/2;
    //                 float r2_yMax = rt2.position.y + rt2.rect.height/2;

    //                 float xlen = Mathf.Min(r1_xMax, r2_xMax) - Mathf.Max(r1_xMin, r2_xMin);
    //                 float ylen = Mathf.Min(r1_yMax, r2_yMax) - Mathf.Max(r1_yMin, r2_yMin);
    //                 if (trueOverlap = (xlen * ylen > 0)) break;
    //             }

    //             if (!trueOverlap) pair.Key.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
    //             else pair.Key.GetComponent<UnityEngine.UI.Image>().color = MouseHandler.highlight;
    //         }
    //     }
    // }

    public void EqPartMouseDown(GameObject goTerm, Vector3 mousePosition) {
        AbstractTerm term = termMap[goTerm];
        if (term is ConstantTerm) 
            mover = new Mover(MoveType.Constant, term, mousePosition);
        else if (goTerm == term.gameObjects[0])
            mover = new Mover(MoveType.Coefficient, term, mousePosition);
        else 
            mover = new Mover(MoveType.Variable, term, mousePosition);        
    }

    public void EqPartMouseDrag(GameObject goTerm, Vector3 mousePosition) {
        if (mover != null ) {
            switch (mover.mvType)
            {
            case MoveType.Constant:
                mover.mvTerm.gameObjects[0].transform.position = Camera.main.
                    ScreenToWorldPoint(mousePosition + mover.offset);
                break;

            case MoveType.Coefficient:
                mover.mvTerm.gameObjects[0].transform.position = 
                    Camera.main.ScreenToWorldPoint(mousePosition + mover.offset);

                Vector3 offset2 = mover.mvTerm.positions[1] - mover.mvTerm.positions[0];
                mover.mvTerm.gameObjects[1].transform.position = Camera.main.
                    ScreenToWorldPoint(mousePosition + mover.offset + offset2);
                break;

            case MoveType.Variable:
                mover.mvTerm.gameObjects[1].transform.position = Camera.main.
                    ScreenToWorldPoint(mousePosition + mover.offset);
                break;

            default:
                Debug.Log($"Unrecognized MoveType: {mover.mvType}.");
                break;
            }
        }
    }

    public void EqPartMouseUp(GameObject goTerm, Vector3 mousePosition) {
        StartCoroutine(AnimateRebound(termMap[goTerm]));
        mover = null;
    }

    IEnumerator CombineTerms(Mover mv, AbstractTerm dropTerm, float duration=0.25f)
    {
        // mv.mvTerm.renderTerm(false); // Hide the moving term from view
        bool signFlip = mv.mvTerm.positions[0].x * mv.mvTerm.gameObjects[0].transform.position.x < 0;
        int finalValue = dropTerm.getValue() + (mv.mvTerm.getValue() * (signFlip ? -1 : 1));
        Text updateText = dropTerm.gameObjects[0].transform.Find("Text").GetComponent<Text>();
        bool dropSign = false; // TODO: need to set true for "1st term" on each LH/RH side  

        float elapsed = 0.0f;
        int value = dropTerm.getValue();
        while (elapsed < duration) {
            mv.mvTerm.setTextAlpha(Mathf.Lerp(1.0f, 0.0f, elapsed/duration));
            value = (int)Mathf.Round(Mathf.Lerp((float)dropTerm.getValue(), (float)finalValue, elapsed/duration));

            string sign = value >= 0 ? " + " : " - ";
            if (dropSign) sign = value < 0 ? "- " : "";
            updateText.text = sign + System.Math.Abs(value).ToString();
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return null;
    }

    IEnumerator SeparateTerms(Mover mv, AbstractTerm dropTerm, float duration=0.25f)
    {
        // mv.mvTerm.renderTerm(true);
        bool signFlip = mv.mvTerm.positions[0].x * mv.mvTerm.gameObjects[0].transform.position.x < 0;
        int initialValue = dropTerm.getValue() + (mv.mvTerm.getValue() * (signFlip ? -1 : 1));
        Text updateText = dropTerm.gameObjects[0].transform.Find("Text").GetComponent<Text>();
        bool dropSign = false; // TODO: need to set true for "1st term" on each LH/RH side  

        float elapsed = 0;
        int value = dropTerm.getValue();
        while (elapsed < duration) {
            mv.mvTerm.setTextAlpha(Mathf.Lerp(0.0f, 1.0f, elapsed/duration));
            value = (int)Mathf.Round(Mathf.Lerp((float)initialValue, (float)dropTerm.getValue(), elapsed/duration));

            string sign = value >= 0 ? " + " : " - ";
            if (dropSign) sign = value < 0 ? "- " : "";
            updateText.text = sign + System.Math.Abs(value).ToString();
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return null;
    }

    IEnumerator AnimateRebound(AbstractTerm t, float duration=0.25f)
    {        
        float elapsed = 0;
        Vector3[] outPos = new Vector3[t.gameObjects.Count];
        for (int i=0; i<t.gameObjects.Count; i++)
            outPos[i] = t.gameObjects[i].transform.position;

        while (elapsed < duration) {
            for (int i=0; i<t.gameObjects.Count; i++)
                t.gameObjects[i].transform.position = Vector3.Lerp(
                    outPos[i], t.positions[i], elapsed/duration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Assure final "snap" into place
        for (int i=0; i<t.gameObjects.Count; i++)
            t.gameObjects[i].transform.position = t.positions[i];

        yield return null;
    }
}
