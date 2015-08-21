using UnityEngine;
using System.Collections;

public class NegativeBinomialDistribution {

	/// <summary>
	/// Returns a random integer with a negative binomial distribution and the given values of r and p.
	/// Attempts are made with probability p of success until there are r failures. The number of successes is returned.
	/// </summary>
	/// <returns>A random integer with a negative binomial distribution and the given values of r and p.</returns>
	/// <param name="r">The number of failures.</param>
	/// <param name="p">The probability of success.</param>
	public static int fromRAndP (int r, float p) {
		int successes = 0;
		int failures = 0;
		while (failures < r) {
			if (Random.value < p) {
				++successes;
			} else {
				++failures;
			}
		}
		return successes;
	}
	
	/// <summary>
	/// Returns a random integer with a negative binomial distribution with a mean of mu and a standard deviation near (but not necessarily equal) sigma.
	/// </summary>
	/// <returns>A random integer with a negative binomial distribution with a mean of mu and a standard deviation near (but not necessarily equal) sigma.</returns>
	/// <param name="mu">The mean.</param>
	/// <param name="sigma">Target standard deviation.</param>
	public static int fromMeanAndStandardDeviation (float mu, float sigma) {
		int r = Mathf.RoundToInt (mu*mu/(sigma*sigma-mu));
		if (r < 2) {
			r = 2;
		}
		float p = mu/(mu+r);
		return fromRAndP (r,p);
	}
}
