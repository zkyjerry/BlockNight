// Offline-only managed stand-ins; never imported into Assets or used as Unity runtime evidence.
using System;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;public static void DestroyImmediate(Object o){}}
 public class ScriptableObject:Object {public static T CreateInstance<T>() where T:ScriptableObject,new()=>new T();}
 public class CreateAssetMenuAttribute:Attribute {public string menuName,fileName;}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 public class MinAttribute:Attribute {public MinAttribute(float n){}}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
 public struct Vector2 {
  public float x,y;public Vector2(float a,float b){x=a;y=b;}
  public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);public static Vector2 up=>new Vector2(0,1);public static Vector2 down=>new Vector2(0,-1);public static Vector2 right=>new Vector2(1,0);public static Vector2 left=>new Vector2(-1,0);
  public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector2 normalized=>magnitude>1e-6?this/magnitude:zero;
  public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 operator /(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);public static bool operator ==(Vector2 a,Vector2 b)=>(a-b).sqrMagnitude<1e-10f;public static bool operator !=(Vector2 a,Vector2 b)=>!(a==b);public override bool Equals(object o)=>o is Vector2 v&&this==v;public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode();
  public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
 }
 public struct Vector2Int {
  public int x,y;public Vector2Int(int a,int b){x=a;y=b;}
  public static Vector2Int zero=>new Vector2Int();public static Vector2Int right=>new Vector2Int(1,0);public static Vector2Int up=>new Vector2Int(0,1);
  public static Vector2Int operator +(Vector2Int a,Vector2Int b)=>new Vector2Int(a.x+b.x,a.y+b.y);public static Vector2Int operator -(Vector2Int a,Vector2Int b)=>new Vector2Int(a.x-b.x,a.y-b.y);public static Vector2Int operator -(Vector2Int a)=>new Vector2Int(-a.x,-a.y);public static bool operator ==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;public static bool operator !=(Vector2Int a,Vector2Int b)=>!(a==b);public static implicit operator Vector2(Vector2Int a)=>new Vector2(a.x,a.y);public override bool Equals(object o)=>o is Vector2Int v&&this==v;public override int GetHashCode()=>x*397^y;
 }
 public static class Mathf {
  public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(params int[] a){int r=a[0];foreach(int x in a)r=Math.Min(r,x);return r;}
  public static float Abs(float x)=>Math.Abs(x);public static int Abs(int x)=>Math.Abs(x);public static float Clamp(float x,float a,float b)=>Max(a,Min(b,x));public static int Clamp(int x,int a,int b)=>Max(a,Min(b,x));public static float Clamp01(float x)=>Clamp(x,0,1);public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static int RoundToInt(float x)=>(int)Math.Round(x);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);
 }
}
