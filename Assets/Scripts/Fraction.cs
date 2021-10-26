public class Fraction
{
    // Sign carried on numerator
    public int num { get; private set; }
    public int den { get; private set; }
    
    public Fraction(Fraction numerator, Fraction denominator, bool _normalize=true) 
        : this(numerator.num * denominator.den, numerator.den * denominator.num, _normalize) {}
    
    public Fraction(Fraction numerator, int denominator, bool _normalize=true) 
        : this(numerator.num, numerator.den * denominator, _normalize) {}
    
    public Fraction(int numerator=0, int denominator=1, bool _normalize=true) {
        num = numerator; den = denominator;
        if (den == 0) throw new System.DivideByZeroException();
        if (den < 0) { num *= -1; den *= -1; }
        if (_normalize) {
            int gcd = GCD(System.Math.Abs(num), den);
            num /= gcd; den /= gcd;
        }
    }
    
    public static Fraction operator +(Fraction a) => a;
    public static Fraction operator -(Fraction a) => new Fraction(-a.num, a.den);

    public static Fraction operator +(Fraction a, Fraction b)
        => new Fraction(a.num * b.den + b.num * a.den, a.den * b.den);
    
    public static Fraction operator -(Fraction a, Fraction b)
        => new Fraction(a.num * b.den - b.num * a.den, a.den * b.den);
    
    public static Fraction operator *(Fraction a, Fraction b)
        => new Fraction(a.num * b.num, a.den * b.den);

    public static Fraction operator /(Fraction a, Fraction b)
        => new Fraction(a.num * b.den, a.den * b.num);

    public static Fraction operator *(Fraction a, int b)
        => new Fraction(a.num * b, a.den);

    public static Fraction operator /(Fraction a, int b)
        => new Fraction(a.num, a.den * b);

    public static Fraction operator %(Fraction a, Fraction b)
        => new Fraction((a.num * b.den) % (b.num * a.den), a.den * b.den);
    
    public static bool operator ==(Fraction a, Fraction b) => a.num == b.num && a.den == b.den;
    public static bool operator ==(Fraction a, int b) => a.num == b && a.den == 1;
    
    public static bool operator !=(Fraction a, Fraction b) => !(a == b);
    public static bool operator !=(Fraction a, int b) => !(a == b);

    public static bool operator <(Fraction a, Fraction b) => a.num/a.den < b.num/b.den;
    public static bool operator <(Fraction a, double b) => a.num/a.den < b;
    
    public static bool operator <=(Fraction a, Fraction b) => a.num/a.den <= b.num/b.den;
    public static bool operator <=(Fraction a, double b) => a.num/a.den <= b;
    
    public static bool operator >(Fraction a, Fraction b) => a.num/a.den > b.num/b.den;
    public static bool operator >(Fraction a, double b) => a.num/a.den > b;
    
    public static bool operator >=(Fraction a, Fraction b) => a.num/a.den >= b.num/b.den;
    public static bool operator >=(Fraction a, double b) => a.num/a.den >= b;
    
    public Fraction Abs() => new Fraction(System.Math.Abs(num), den);

    public Fraction Pow(int b) => new Fraction(
        (int)System.Math.Pow(num, b), 
        (int)System.Math.Pow(den, b));
    
    // public override string ToString() => $"Fraction({num}, {den})";    
    public override string ToString() => den == 1 ? $"{num}" : $"{num}/{den}";
    public override bool Equals(object obj) => (obj is Fraction) && (this == (Fraction)obj);
    public override int GetHashCode() => ((double)num/den).GetHashCode();

    private static int GCD(int a, int b) {
        //Debug.Assert(a >= 0 && b >= 0);
        while (a != 0 && b != 0) {
            if (a > b) a %= b;
            else b %= a;
        }
        return a | b;
    }
}