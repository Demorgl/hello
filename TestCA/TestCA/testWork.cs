using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestWork
{
    //-------------------------------------------------------
    public interface ITriangle
    {
        double SideA { get; set; }
        double SideB { get; set; }
        double SideC { get; set; }
    }

    public interface IMath
    {
        double CalcTriangleSquare(ITriangle triangle);
    }

    //-------------------------------------------------------------
    public class MyMath : IMath
    {
        public double CalcTriangleSquare(ITriangle triangle)
        {
 	        double p = (triangle.SideA + triangle.SideB + triangle.SideC) / 2;
 
            return Math.Sqrt(p * (p - triangle.SideA) * (p - triangle.SideB) * (p - triangle.SideC));
        }
    }
    
    public class Triangle : ITriangle
    {
        private double sideA;
        private double sideB;
        private double sideC;

        public double SideA
        {
            get
            {
                return this.sideA;
            }
            set
            {
                if (IsPositive(value))
                    this.sideA = value;
                else
                    throw new ArgumentOutOfRangeException("SideA", value, "Значение стороны треугольника A меньше либо равно нулю");
            }
        }

        public double SideB
        {
            get
            {
                return this.sideB;
            }
            set
            {
                if (IsPositive(value))
                    this.sideB = value;
                else
                    throw new ArgumentOutOfRangeException("SideB", value, "Значение стороны треугольника B меньше либо равно нулю");
            }
        }

        public double SideC
        {
            get
            {
                return this.sideC;
            }
            set
            {
                if (IsPositive(value))
                    this.sideC = value;
                else
                    throw new ArgumentOutOfRangeException("SideC", value, "Значение стороны треугольника C меньше либо равно нулю");
            }
        }

        private bool IsPositive(double value)
        {
            return value > 0;
        }
    }
    
    //---------------------------------------------------------------
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TriangleNegativeSideA_ThowArgumentOutOfRange()
        {
            double sideA = 10;
            Triangle triangle = new Triangle();

            triangle.SideA = sideA;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TriangleNegativeSideB_ThowArgumentOutOfRange()
        {
            double sideB = 10;
            Triangle triangle = new Triangle();

            triangle.SideB = sideB;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TriangleNegativeSideC_ThowArgumentOutOfRange()
        {
            double sideC = 10;
            Triangle triangle = new Triangle();

            triangle.SideC = sideC;
        }
    }
}
