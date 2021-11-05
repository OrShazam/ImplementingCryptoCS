using System;
using System.Collections.Generic;
// in development
namespace Crypto
{
    sealed class Utils
    {
        public byte[] hexStringToBytes(string hex)
        {
            List<byte> bytes = new List<byte>();
            int curr = 0;
            for(int i = 2; i < hex.Length; hex += 2)
            {
                // begins with 0x
                for (int j = i; j < i+2; j++)
                {
                    if (hex[j] >= 'a')
                        curr += hex[j] - 'a' + 10;
                    else 
                        curr += hex[j] - '0';
                    if (j % 2 == 0)
                        curr *= 16;
                }
                bytes.Add((byte)curr);
                curr = 0;
            }
            return bytes.ToArray();

        }
    }
    sealed class CurvePoint : IEquatable<CurvePoint>
    {
        // reread the infinity thing
        private FieldElement x; private FieldElement y;
        private FieldElement a; private FieldElement b;
        public FieldElement getA()
        {
            return a;
        }
        public FieldElement getB()
        {
            return b;
        }
        public FieldElement getX()
        {
            return x;
        }
        public FieldElement getY()
        {
            return y;
        }
        private bool SameCurve(CurvePoint other)
        {
            return a == other.a && b == other.b;
        }
        private bool isOnCurve(FieldElement a, FieldElement b, FieldElement x, FieldElement y)
        {
            if (x == null)
                return true; // identity point 
            return y.Pow(2) == x.Pow(3) + a * x + b;
        }
        public CurvePoint(FieldElement a, FieldElement b, FieldElement x, FieldElement y)
        {
            /*if (!isOnCurve(a, b, x, y))
            {
                throw new Exception("Point is not on the specified curve");
            }*/
            this.a = a; this.b = b; this.x = x; this.y = y;
        }
        public bool Equals(CurvePoint other)
        {
            return SameCurve(other) && x == other.x && y == other.y;
        }
        public override string ToString()
        {
            // x or y can be null
            return $"X: {x.GetIndex()} Y: {y.GetIndex()} Curve: y^2 = x^3 + " +
                $"({a.GetIndex()})x + ({b.GetIndex()})";
        }
        public static bool operator == (CurvePoint cp1, CurvePoint cp2)
        {
            return cp1.Equals(cp2);
        }
        public static bool operator != (CurvePoint cp1, CurvePoint cp2)
        {
            return !cp1.Equals(cp2);
        }
        public static CurvePoint operator + (CurvePoint cp1, CurvePoint cp2)
        {
            if (!cp1.SameCurve(cp2))
                return null;
            if (cp1.getX() == null)
                return cp2;
            if (cp2.getX() == null)
                return cp1;
            FieldElement _2 = new FieldElement(cp1.getX().GetOrder(), 2);
            FieldElement _3 = new FieldElement(cp1.getX().GetOrder(), 3);
            if (cp1.getX() == cp2.getX())
            {
                // infinity point
                if (cp1.getY() != cp2.getY())
                    return new CurvePoint(cp1.getA(), cp1.getB(), null, null);
                // tangent point 
                else
                {
                    if (cp1.getY().GetIndex() == 0)
                        // vertical line
                        return new CurvePoint(cp1.getA(), cp1.getB(), null, null);
                    FieldElement slope = (_3 * cp1.getX().Pow(2) + cp1.getA()) /
                        (_2 * cp1.getY());
                    if (slope == null)
                    {
                        return new CurvePoint(cp1.getA(), cp1.getB(), null, null);
                    }
                    FieldElement x3 = slope.Pow(2) - _2 * cp1.getX();
                    FieldElement y3 = slope * (cp1.getX() - x3) - cp1.getY();
                    return new CurvePoint(cp1.getA(), cp1.getB(), x3, y3);
                }
            }
            else
            {
                FieldElement slope = (cp2.getY() - cp1.getY())
               / (cp2.getX() - cp1.getX());
                FieldElement x3 = slope.Pow(2) - cp1.getX() - cp2.getX();
                FieldElement y3 = slope * (cp1.getX() - x3) - cp1.getY();
                return new CurvePoint(cp1.getA(), cp1.getB(), x3, y3);
            }

        }
        /*public int FindOrder()
        {
            CurvePoint infinity = new CurvePoint(a, b, null, null);
            CurvePoint cpTemp = this;
            int count = 1;
            while (cpTemp != infinity)
            {
                cpTemp += this;
                count++;
            }
            return count;
        }*/
        public static CurvePoint operator * (CurvePoint cp1, int scalar)
        {
            // same technique as efficientModPow basically
            CurvePoint result = new CurvePoint(cp1.getA(), cp1.getB(), null, null);
            int scalarTemp = scalar;
            CurvePoint cp1Temp = cp1;
            while (scalarTemp > 0)
            {
                if ((scalarTemp & 1) == 1)
                    result += cp1Temp;
                cp1Temp += cp1Temp;
                scalarTemp >>= 1;
            }
            return result;
        }
    }
    sealed class FieldElement : IEquatable<FieldElement>
    {
        private int efficientModPow(int num, int pow, int mod)
        {
            // I: (a * b) % c = ((a % c) * (b%c)) % c
            // II: a = b0 * 2 ** 0 + b1 * 2 ** 1 + ...
            // a ^ b % c = a ^ (b0 * 2 ** 0 + ...) % c 
            // = a ^ (b0 * 2 ** 0) * a ^ (b1 * 2 ** 1) % c 
            // = (a ^ (b0 * 2 ** 0) % c * ...) % c
            int result = 1;
            while (pow > 0)
            {
                if ((pow & 1) == 1) // check if lower bit set
                    result = result * num % mod;
                pow >>= 1; // go to next bit
                num = num * num % mod;         
            }
            return result;

        }
        private int order;
        private int index;
        public int GetOrder()
        {
            return order;
        }
        public int GetIndex()
        {
            return index;
        }
        public bool SameOrder(FieldElement other)
        { 
            return order == other.GetOrder();
        }
        public FieldElement(int order, int index)
        {
           if (index >= order || index < 0)
            {
                throw new Exception
                    ("index has to be less than order and greater than 0");
            } 
            this.order = order; this.index = index;
        }
        public bool Equals(FieldElement other)
        {
            return SameOrder(other) && GetIndex() == other.GetIndex();
        }
        public override string ToString()
        {
            return $"Element {index} in finite field of order {order}";

        }
        public static FieldElement operator +(FieldElement fe1, FieldElement fe2)
        {
            if (!fe1.SameOrder(fe2))
                return null;
            int order = fe1.GetOrder();
            return new FieldElement(order, (fe1.GetIndex() + fe2.GetIndex()) % order);
        }
        public static FieldElement operator -(FieldElement fe1, FieldElement fe2)
        {
            if (!fe1.SameOrder(fe2))
                return null;
            int order = fe1.GetOrder();
            int resultingIndex = (fe1.GetIndex() - fe2.GetIndex()) % order;
            if (resultingIndex < 0)
                resultingIndex += order;
            return new FieldElement(order, resultingIndex);
        }
        public static bool operator ==(FieldElement fe1, FieldElement fe2)
        {
            if ((object)fe1 == null && (object)fe2 == null)
                return true;
            if ((object)fe1 == null || (object)fe2 == null)
                return false;

            return fe1.Equals(fe2);
        }
        public static bool operator !=(FieldElement fe1, FieldElement fe2)
        {
            return !fe1.Equals(fe2);
        }
        public static FieldElement operator *(FieldElement fe1, FieldElement fe2)
        {
            if (!fe1.SameOrder(fe2))
                return null;
            int order = fe1.GetOrder();
            return new FieldElement(order, (fe1.GetIndex() * fe2.GetIndex()) % order);
        }
        public FieldElement Pow(int power)
        {
            int modifiedPower = power;
            // for p prime
            // (a^b) % p = (a ^ b * 1) % p = (a ^ b * a ^ (p -1)) % p = 
            // = (a ^ (b + p -1)) % p ...
            while (modifiedPower < 0)
            {
                modifiedPower += order - 1;
            }
            modifiedPower %= (order - 1);
            return new FieldElement(order, efficientModPow(index, modifiedPower, order));
        }
        public static FieldElement operator /(FieldElement fe1, FieldElement fe2)
        {
            if (!fe1.SameOrder(fe2))
                return null;
            // for p prime
            // n^(p-1) % p = 1, (a / b) % p = (a * (1/b)) % p =
            // = (a * b^(p-1)/b) % p = (a * b^(p-2) % p
            return fe1 * fe2.Pow(fe1.GetOrder() - 2);
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            // use hexToBytes to get bytes of public/private key, and change fieldElement to use BigInteger
           
        }
    }
}