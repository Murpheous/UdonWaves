using UnityEngine;

public static class Units 
{
    public static string Eng(float x, string format="g")
    {
        const string sup_signs = "⁺⁻⁼⁽⁾ⁿ";
        const string sup_digits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

        if(double.IsNaN(x) || double.IsInfinity(x))
        {
            return x.ToString();
        }

        int num_sign = (int)Mathf.Sign(x);
        x = Mathf.Abs(x);
        // group exponents in multiples of 3 (thousands)
        int exp = (int)Mathf.Floor(Mathf.Log(x, 10)/3)*3;
        // otherwise use:
        // int exp = (int)Math.Floor(Math.Log(x, 10));
        // and handle the exp==1 case separetly to avoid 10¹
        x*= Mathf.Pow(10, -exp);
        int exp_sign = (int)Mathf.Sign(exp);
        exp = Mathf.Abs(exp);
        // Build the exponent string 'dig' from right to left
        string dig = string.Empty;
        while(exp>0)
        {
            int n = exp%10;
            dig = sup_digits[n] + dig;
            exp = exp/10;
        }
        // if has exponent and its negative prepend the superscript minus sign
        if(dig.Length>0 && exp_sign<0)
        {
            dig = sup_signs[1] + dig;
        }
        // prepend answer with minus if number is negative
        string sig = num_sign<0 ? "-" : "";            
        if(dig.Length>0)
        {
            // has exponent
            return $"{sig}{x.ToString(format)}×10{dig}";
        }
        else
        {
            // no exponent
            return $"{sig}{x.ToString(format)}";
        }
    }

    public static string ToEngineeringNotation(float d)
	{
		string sRet = "0";
		float exponent = Mathf.Log10(Mathf.Abs(d));
		if (Mathf.Abs(d) >= 1)
		{
			switch ((int)Mathf.Floor(exponent))
			{
			case 0: case 1: case 2:
				return d.ToString();
			case 3: case 4: case 5:
				return (d / 1e3).ToString() + "k";
			case 6: case 7: case 8:
				return (d / 1e6).ToString() + "M";
			case 9: case 10: case 11:
				return (d / 1e9).ToString() + "G";
			case 12: case 13: case 14:
				return (d / 1e12).ToString() + "T";
			case 15: case 16: case 17:
				return (d / 1e15).ToString() + "P";
			case 18: case 19: case 20:
				return (d / 1e18).ToString() + "E";
			case 21: case 22: case 23:
				return (d / 1e21).ToString() + "Z";
			default:
				return (d / 1e24).ToString() + "Y";
			}
		}
		else if (Mathf.Abs(d) > 0)
		{
			switch ((int)Mathf.Floor(exponent))
			{
			case -1: case -2: case -3:
				sRet =  string.Format("{0:.#}m",(d * 1e3));
				break;
			case -4: case -5: case -6:
				sRet =  string.Format("{0:.#}μ",(d * 1e6));
				break;
			case -7: case -8: case -9:
				sRet =  string.Format("{0:.#}n",(d * 1e9));
				break;
			case -10: case -11: case -12:
				return (d * 1e12).ToString() + "p";
			case -13: case -14: case -15:
				return (d * 1e15).ToString() + "f";
			case -16: case -17: case -18:
				return (d * 1e15).ToString() + "a";
			case -19: case -20: case -21:
				return (d * 1e15).ToString() + "z";
			default:
				return (d * 1e15).ToString() + "y";
			}
		}
		return sRet;
	}
	
    // Start is called before the first frame update

}
