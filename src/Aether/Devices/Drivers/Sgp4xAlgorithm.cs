namespace Aether.Devices.Drivers
{
    using fix16_t = Int32;


    internal class Sgp4xAlgorithm
    {
        const double VocAlgorithm_SAMPLING_INTERVAL = 1;
        const double VocAlgorithm_INITIAL_BLACKOUT = 45;
        const double VocAlgorithm_VOC_INDEX_GAIN = 230;
        const double VocAlgorithm_SRAW_STD_INITIAL = 50;
        const double VocAlgorithm_SRAW_STD_BONUS = 220;
        const double VocAlgorithm_TAU_MEAN_VARIANCE_HOURS = 12;
        const double VocAlgorithm_TAU_INITIAL_MEAN = 20;
        const double VocAlgorithm_INIT_DURATION_MEAN = 3600 * 0.75;
        const double VocAlgorithm_INIT_TRANSITION_MEAN = 0.01;
        const double VocAlgorithm_TAU_INITIAL_VARIANCE = 2500;
        const double VocAlgorithm_INIT_DURATION_VARIANCE = 3600 * 1.45;
        const double VocAlgorithm_INIT_TRANSITION_VARIANCE = 0.01;
        const double VocAlgorithm_GATING_THRESHOLD = 340;
        const double VocAlgorithm_GATING_THRESHOLD_INITIAL = 510;
        const double VocAlgorithm_GATING_THRESHOLD_TRANSITION = 0.09;
        const double VocAlgorithm_GATING_MAX_DURATION_MINUTES = 60 * 3;
        const double VocAlgorithm_GATING_MAX_RATIO = 0.3;
        const double VocAlgorithm_SIGMOID_L = 500;
        const double VocAlgorithm_SIGMOID_K = -0.0065;
        const double VocAlgorithm_SIGMOID_X0 = 213;
        const double VocAlgorithm_VOC_INDEX_OFFSET_DEFAULT = 100;
        const double VocAlgorithm_LP_TAU_FAST = 20;
        const double VocAlgorithm_LP_TAU_SLOW = 500;
        const double VocAlgorithm_LP_ALPHA = -0.2;
        const double VocAlgorithm_PERSISTENCE_UPTIME_GAMMA = 3 * 3600;
        const double VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING = 64;
        const double VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__FIX16_MAX = 32767;


        internal struct VocAlgorithmParams
        {
            public int mVoc_Index_Offset;
            public int mTau_Mean_Variance_Hours;
            public int mGating_Max_Duration_Minutes;
            public int mSraw_Std_Initial;
            public int mUptime;
            public int mSraw;
            public int mVoc_Index;
            public int m_Mean_Variance_Estimator__Gating_Max_Duration_Minutes;
            public bool m_Mean_Variance_Estimator___Initialized;
            public int m_Mean_Variance_Estimator___Mean;
            public int m_Mean_Variance_Estimator___Sraw_Offset;
            public int m_Mean_Variance_Estimator___Std;
            public int m_Mean_Variance_Estimator___Gamma;
            public int m_Mean_Variance_Estimator___Gamma_Initial_Mean;
            public int m_Mean_Variance_Estimator___Gamma_Initial_Variance;
            public int m_Mean_Variance_Estimator__Gamma_Mean;
            public int m_Mean_Variance_Estimator__Gamma_Variance;
            public int m_Mean_Variance_Estimator___Uptime_Gamma;
            public int m_Mean_Variance_Estimator___Uptime_Gating;
            public int m_Mean_Variance_Estimator___Gating_Duration_Minutes;
            public int m_Mean_Variance_Estimator___Sigmoid__L;
            public int m_Mean_Variance_Estimator___Sigmoid__K;
            public int m_Mean_Variance_Estimator___Sigmoid__X0;
            public int m_Mox_Model__Sraw_Std;
            public int m_Mox_Model__Sraw_Mean;
            public int m_Sigmoid_Scaled__Offset;
            public int m_Adaptive_Lowpass__A1;
            public int m_Adaptive_Lowpass__A2;
            public bool m_Adaptive_Lowpass___Initialized;
            public int m_Adaptive_Lowpass___X1;
            public int m_Adaptive_Lowpass___X2;
            public int m_Adaptive_Lowpass___X3;
        };
        /*
 * Copyright (c) 2021, Sensirion AG
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 *
 * * Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 *
 * * Neither the name of Sensirion AG nor the names of its
 *   contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */


        /* The fixed point arithmetic parts of this code were originally created by
         * https://github.com/PetteriAimonen/libfixmath
         */

        /*!< the maximum value of fix16_t */
        const int FIX16_MAXIMUM = 0x7FFFFFFF;
        /*!< the minimum value of fix16_t */
        const uint FIX16_MINIMUM = 0x80000000;
        /*!< the value used to indicate overflows when FIXMATH_NO_OVERFLOW is not
         * specified */
        const uint FIX16_OVERFLOW = 0x80000000;
        /*!< fix16_t value of 1 */
        const int FIX16_ONE = 0x00010000;

        static fix16_t F16(double x) => (fix16_t)(((x) >= 0) ? ((x) * 65536.0 + 0.5) : ((x) * 65536.0 - 0.5));

        static fix16_t fix16_from_int(int a)
        {
            return a * FIX16_ONE;
        }

        static int fix16_cast_to_int(fix16_t a)
        {

            return (a >= 0) ? (a >> 16) : -((-a) >> 16);
        }

        /*! Multiplies the two given fix16_t's and returns the result. */
        static fix16_t fix16_mul(fix16_t inArg0, fix16_t inArg1)
        {
            // Each argument is divided to 16-bit parts.
            //					AB
            //			*	 CD
            // -----------
            //					BD	16 * 16 -> 32 bit products
            //				 CB
            //				 AD
            //				AC
            //			 |----| 64 bit product
            uint absArg0 = (uint)((inArg0 >= 0) ? inArg0 : (-inArg0));
            uint absArg1 = (uint)((inArg1 >= 0) ? inArg1 : (-inArg1));
            uint A = (absArg0 >> 16), C = (absArg1 >> 16);
            uint B = (absArg0 & 0xFFFF), D = (absArg1 & 0xFFFF);

            uint AC = A * C;
            uint AD_CB = A * D + C * B;
            uint BD = B * D;

            uint product_hi = AC + (AD_CB >> 16);

            // Handle carry from lower 32 bits to upper part of result.
            uint ad_cb_temp = AD_CB << 16;
            uint product_lo = BD + ad_cb_temp;
            if (product_lo < BD)
                product_hi++;

#if !FIXMATH_NO_OVERFLOW
            // The upper 17 bits should all be zero.
            if (product_hi >> 15 == 1)
                unchecked
                {
                    return (fix16_t)FIX16_OVERFLOW;
                }

#endif

#if FIXMATH_NO_ROUNDING
            fix16_t result = (fix16_t)((product_hi << 16) | (product_lo >> 16));
            if ((inArg0 < 0) != (inArg1 < 0))
                result = -result;
            return result;
#else
            // Adding 0x8000 (= 0.5) and then using right shift
            // achieves proper rounding to result.
            // Handle carry from lower to upper part.
            uint product_lo_tmp = product_lo;
            product_lo += 0x8000;
            if (product_lo < product_lo_tmp)
                product_hi++;

            // Discard the lowest 16 bits and convert back to signed result.
            fix16_t result = (fix16_t)((product_hi << 16) | (product_lo >> 16));
            if ((inArg0 < 0) != (inArg1 < 0))
                result = -result;
            return result;
#endif
        }

        /*! Divides the first given fix16_t by the second and returns the result. */
        static fix16_t fix16_div(fix16_t a, fix16_t b)
        {
            // This uses the basic binary restoring division algorithm.
            // It appears to be faster to do the whole division manually than
            // trying to compose a 64-bit divide out of 32-bit divisions on
            // platforms without hardware divide.

            if (b == 0)
                unchecked
                {
                    return (fix16_t)FIX16_MINIMUM;
                }


            uint remainder = (uint)((a >= 0) ? a : (-a));
            uint divider = (uint)((b >= 0) ? b : (-b));

            uint quotient = 0;
            uint bit = 0x10000;

            /* The algorithm requires D >= R */
            while (divider < remainder)
            {
                divider <<= 1;
                bit <<= 1;
            }

#if !FIXMATH_NO_OVERFLOW
            if (bit == 0)
                unchecked
                {
                    return (fix16_t)FIX16_OVERFLOW;
                }
#endif

            if ((divider & 0x80000000) == 1)
            {
                // Perform one step manually to avoid overflows later.
                // We know that divider's bottom bit is 0 here.
                if (remainder >= divider)
                {
                    quotient |= bit;
                    remainder -= divider;
                }
                divider >>= 1;
                bit >>= 1;
            }

            /* Main division loop */
            while (bit == 1 && remainder == 1)
            {
                if (remainder >= divider)
                {
                    quotient |= bit;
                    remainder -= divider;
                }

                remainder <<= 1;
                bit >>= 1;
            }

#if !FIXMATH_NO_ROUNDING
            if (remainder >= divider)
            {
                quotient++;
            }
#endif

            fix16_t result = (fix16_t)quotient;

            /* Figure out the sign of result */
            if ((a < 0) != (b < 0))
            {
#if !FIXMATH_NO_OVERFLOW
#pragma warning disable CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type
                if (result == FIX16_MINIMUM)
#pragma warning restore CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type
                    unchecked
                    {
                        return (fix16_t)FIX16_OVERFLOW;
                    }

#endif

                result = -result;
            }

            return result;
        }

        /*! Returns the square root of the given fix16_t. */
        static fix16_t fix16_sqrt(fix16_t x)
        {
            // It is assumed that x is not negative

            uint num = (uint)x;
            uint result = 0;
            uint bit;
            byte n;

            bit = (uint)1 << 30;
            while (bit > num)
                bit >>= 2;

            // The main part is executed twice, in order to avoid
            // using 64 bit values in computations.
            for (n = 0; n < 2; n++)
            {
                // First we get the top 24 bits of the answer.
                while (bit != 0)
                {
                    if (num >= result + bit)
                    {
                        num -= result + bit;
                        result = (result >> 1) + bit;
                    }
                    else
                    {
                        result = (result >> 1);
                    }
                    bit >>= 2;
                }

                if (n == 0)
                {
                    // Then process it again to get the lowest 8 bits.
                    if (num > 65535)
                    {
                        // The remainder 'num' is too large to be shifted left
                        // by 16, so we have to add 1 to result manually and
                        // adjust 'num' accordingly.
                        // num = a - (result + 0.5)^2
                        //	 = num + result^2 - (result + 0.5)^2
                        //	 = num - result - 0.5
                        num -= result;
                        num = (num << 16) - 0x8000;
                        result = (result << 16) + 0x8000;
                    }
                    else
                    {
                        num <<= 16;
                        result <<= 16;
                    }

                    bit = 1 << 14;
                }
            }

#if !FIXMATH_NO_ROUNDING
            // Finally, if next bit would have been 1, round the result upwards.
            if (num > result)
            {
                result++;
            }
#endif

            return (fix16_t)result;
        }

        /*! Returns the exponent (e^) of the given fix16_t. */
        static fix16_t fix16_exp(fix16_t x)
        {
            // Function to approximate exp(); optimized more for code size than speed

            // exp(x) for x = +/- {1, 1/8, 1/64, 1/512}
            const int NUM_EXP_VALUES = 4;
            fix16_t[] exp_pos_values = new fix16_t[NUM_EXP_VALUES] { F16(2.7182818), F16(1.1331485), F16(1.0157477), F16(1.0019550) };
            fix16_t[] exp_neg_values = new fix16_t[NUM_EXP_VALUES] { F16(0.3678794), F16(0.8824969), F16(0.9844964), F16(0.9980488) };

            fix16_t[] exp_values;

            fix16_t res, arg;
            ushort i;

            if (x >= F16(10.3972))
                return FIX16_MAXIMUM;
            if (x <= F16(-11.7835))
                return 0;

            if (x < 0)
            {
                x = -x;
                exp_values = exp_neg_values;
            }
            else
            {
                exp_values = exp_pos_values;
            }

            res = FIX16_ONE;
            arg = FIX16_ONE;
            for (i = 0; i < NUM_EXP_VALUES; i++)
            {
                while (x >= arg)
                {
                    res = fix16_mul(res, exp_values[i]);
                    x -= arg;
                }
                arg >>= 3;
            }
            return res;
        }

        public static void VocAlgorithm_init(VocAlgorithmParams algoParams)
        {
            algoParams.mVoc_Index_Offset = F16(VocAlgorithm_VOC_INDEX_OFFSET_DEFAULT);
            algoParams.mTau_Mean_Variance_Hours = F16(VocAlgorithm_TAU_MEAN_VARIANCE_HOURS);
            algoParams.mGating_Max_Duration_Minutes = F16(VocAlgorithm_GATING_MAX_DURATION_MINUTES);
            algoParams.mSraw_Std_Initial = F16(VocAlgorithm_SRAW_STD_INITIAL);
            algoParams.mUptime = F16(0);
            algoParams.mSraw = F16(0);
            algoParams.mVoc_Index = 0;
            VocAlgorithm__init_instances(algoParams);
        }

        public static void VocAlgorithm__init_instances(VocAlgorithmParams algoParams)
        {

            VocAlgorithm__mean_variance_estimator__init(algoParams);
            VocAlgorithm__mean_variance_estimator__set_parameters(
                algoParams, algoParams.mSraw_Std_Initial, algoParams.mTau_Mean_Variance_Hours,
                algoParams.mGating_Max_Duration_Minutes);
            VocAlgorithm__mox_model__init(algoParams);
            VocAlgorithm__mox_model__set_parameters(
                algoParams, VocAlgorithm__mean_variance_estimator__get_std(algoParams),
        VocAlgorithm__mean_variance_estimator__get_mean(algoParams));
            VocAlgorithm__sigmoid_scaled__init(algoParams);
            VocAlgorithm__sigmoid_scaled__set_parameters(algoParams,
                                                         algoParams.mVoc_Index_Offset);
            VocAlgorithm__adaptive_lowpass__init(algoParams);
            VocAlgorithm__adaptive_lowpass__set_parameters(algoParams);
        }

        public void VocAlgorithm_get_states(VocAlgorithmParams algoParams, out int state0,
                                     out int state1)
        {

            state0 = VocAlgorithm__mean_variance_estimator__get_mean(algoParams);
            state1 = VocAlgorithm__mean_variance_estimator__get_std(algoParams);
            return;
        }

        public void VocAlgorithm_set_states(VocAlgorithmParams algoParams, int state0,
                                     int state1)
        {

            VocAlgorithm__mean_variance_estimator__set_states(
                algoParams, state0, state1, F16(VocAlgorithm_PERSISTENCE_UPTIME_GAMMA));
            algoParams.mSraw = state0;
        }

        public void VocAlgorithm_set_tuning_parameters(VocAlgorithmParams algoParams,
                                                int voc_index_offset,
                                                int learning_time_hours,
                                                int gating_max_duration_minutes,
                                                int std_initial)
        {

            algoParams.mVoc_Index_Offset = (fix16_from_int(voc_index_offset));
            algoParams.mTau_Mean_Variance_Hours = (fix16_from_int(learning_time_hours));
            algoParams.mGating_Max_Duration_Minutes = (fix16_from_int(gating_max_duration_minutes));
            algoParams.mSraw_Std_Initial = (fix16_from_int(std_initial));
            VocAlgorithm__init_instances(algoParams);
        }

        public static void VocAlgorithm_process(VocAlgorithmParams algoParams, int sraw, out int voc_index)
        {

            if (false)//(algoParams.mUptime <= F16(VocAlgorithm_INITIAL_BLACKOUT)))
            {
                algoParams.mUptime = (algoParams.mUptime + F16(VocAlgorithm_SAMPLING_INTERVAL));
            }
            else
            {
                if (((sraw > 0) && (sraw < 65000)))
                {
                    if ((sraw < 20001))
                    {
                        sraw = 20001;
                    }
                    else if ((sraw > 52767))
                    {
                        sraw = 52767;
                    }
                    algoParams.mSraw = fix16_from_int(sraw - 20000);
                    Console.WriteLine("UPDATED SRAW: {0}", algoParams.mSraw);
                }
                
                algoParams.mVoc_Index = VocAlgorithm__mox_model__process(algoParams, algoParams.mSraw);
                Console.WriteLine("VOC POST MOX: {0}", algoParams.mVoc_Index);
                algoParams.mVoc_Index = VocAlgorithm__sigmoid_scaled__process(algoParams, algoParams.mVoc_Index);
                Console.WriteLine("VOC POST SCALED: {0}", algoParams.mVoc_Index);
                algoParams.mVoc_Index = VocAlgorithm__adaptive_lowpass__process(algoParams, algoParams.mVoc_Index);
                Console.WriteLine("VOC POST ALP: {0}", algoParams.mVoc_Index);
                if (algoParams.mVoc_Index < F16(0.5))
                {
                    algoParams.mVoc_Index = F16(0.5);
                }
                if (algoParams.mSraw > F16(0))
                {
                    VocAlgorithm__mean_variance_estimator__process(algoParams, algoParams.mSraw, algoParams.mVoc_Index);
                    VocAlgorithm__mox_model__set_parameters(algoParams, VocAlgorithm__mean_variance_estimator__get_std(algoParams),
                VocAlgorithm__mean_variance_estimator__get_mean(algoParams));
                }
            }
            voc_index = (fix16_cast_to_int((algoParams.mVoc_Index + F16(0.5))));
            return;
        }

        public static void VocAlgorithm__mean_variance_estimator__init(VocAlgorithmParams algoParams)
        {
            VocAlgorithm__mean_variance_estimator__set_parameters(algoParams, F16(0), F16(0), F16(0));
            VocAlgorithm__mean_variance_estimator___init_instances(algoParams);
        }

        static void VocAlgorithm__mean_variance_estimator___init_instances(VocAlgorithmParams algoParams)
        {
            VocAlgorithm__mean_variance_estimator___sigmoid__init(algoParams);
        }

        static void VocAlgorithm__mean_variance_estimator__set_parameters(VocAlgorithmParams algoParams, fix16_t std_initial, fix16_t tau_mean_variance_hours, fix16_t gating_max_duration_minutes)
        {

            algoParams.m_Mean_Variance_Estimator__Gating_Max_Duration_Minutes = gating_max_duration_minutes;
            algoParams.m_Mean_Variance_Estimator___Initialized = false;
            algoParams.m_Mean_Variance_Estimator___Mean = F16(0);
            algoParams.m_Mean_Variance_Estimator___Sraw_Offset = F16(0);
            algoParams.m_Mean_Variance_Estimator___Std = std_initial;
            algoParams.m_Mean_Variance_Estimator___Gamma = fix16_div(F16((VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING * (VocAlgorithm_SAMPLING_INTERVAL / 3600))), (tau_mean_variance_hours + F16((VocAlgorithm_SAMPLING_INTERVAL / 3600))));
            algoParams.m_Mean_Variance_Estimator___Gamma_Initial_Mean = F16(((VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING * VocAlgorithm_SAMPLING_INTERVAL) / (VocAlgorithm_TAU_INITIAL_MEAN + VocAlgorithm_SAMPLING_INTERVAL)));
            algoParams.m_Mean_Variance_Estimator___Gamma_Initial_Variance = F16(((VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING * VocAlgorithm_SAMPLING_INTERVAL) / (VocAlgorithm_TAU_INITIAL_VARIANCE + VocAlgorithm_SAMPLING_INTERVAL)));
            algoParams.m_Mean_Variance_Estimator__Gamma_Mean = F16(0);
            algoParams.m_Mean_Variance_Estimator__Gamma_Variance = F16(0);
            algoParams.m_Mean_Variance_Estimator___Uptime_Gamma = F16(0);
            algoParams.m_Mean_Variance_Estimator___Uptime_Gating = F16(0);
            algoParams.m_Mean_Variance_Estimator___Gating_Duration_Minutes = F16(0);
        }

        static void VocAlgorithm__mean_variance_estimator__set_states(VocAlgorithmParams algoParams, fix16_t mean, fix16_t std, fix16_t uptime_gamma)
        {
            algoParams.m_Mean_Variance_Estimator___Mean = mean;
            algoParams.m_Mean_Variance_Estimator___Std = std;
            algoParams.m_Mean_Variance_Estimator___Uptime_Gamma = uptime_gamma;
            algoParams.m_Mean_Variance_Estimator___Initialized = true;
        }

        static fix16_t
        VocAlgorithm__mean_variance_estimator__get_std(VocAlgorithmParams algoParams)
        {
            return algoParams.m_Mean_Variance_Estimator___Std;
        }

        static fix16_t VocAlgorithm__mean_variance_estimator__get_mean(VocAlgorithmParams algoParams)
        {

            return algoParams.m_Mean_Variance_Estimator___Mean + algoParams.m_Mean_Variance_Estimator___Sraw_Offset;
        }

        static void VocAlgorithm__mean_variance_estimator___calculate_gamma(VocAlgorithmParams algoParams, fix16_t voc_index_from_prior)
        {

            fix16_t uptime_limit;
            fix16_t sigmoid_gamma_mean;
            fix16_t gamma_mean;
            fix16_t gating_threshold_mean;
            fix16_t sigmoid_gating_mean;
            fix16_t sigmoid_gamma_variance;
            fix16_t gamma_variance;
            fix16_t gating_threshold_variance;
            fix16_t sigmoid_gating_variance;

            uptime_limit = F16((VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__FIX16_MAX -
                                VocAlgorithm_SAMPLING_INTERVAL));
            if ((algoParams.m_Mean_Variance_Estimator___Uptime_Gamma < uptime_limit))
            {
                algoParams.m_Mean_Variance_Estimator___Uptime_Gamma =
            (algoParams.m_Mean_Variance_Estimator___Uptime_Gamma +
             F16(VocAlgorithm_SAMPLING_INTERVAL));
            }
            if ((algoParams.m_Mean_Variance_Estimator___Uptime_Gating < uptime_limit))
            {
                algoParams.m_Mean_Variance_Estimator___Uptime_Gating =
            (algoParams.m_Mean_Variance_Estimator___Uptime_Gating +
             F16(VocAlgorithm_SAMPLING_INTERVAL));
            }
            VocAlgorithm__mean_variance_estimator___sigmoid__set_parameters(
                algoParams, F16(1), F16(VocAlgorithm_INIT_DURATION_MEAN),
        F16(VocAlgorithm_INIT_TRANSITION_MEAN));
            sigmoid_gamma_mean =
                VocAlgorithm__mean_variance_estimator___sigmoid__process(
                   algoParams, algoParams.m_Mean_Variance_Estimator___Uptime_Gamma);
            gamma_mean =
                (algoParams.m_Mean_Variance_Estimator___Gamma +
                 (fix16_mul((algoParams.m_Mean_Variance_Estimator___Gamma_Initial_Mean -
                             algoParams.m_Mean_Variance_Estimator___Gamma),
                            sigmoid_gamma_mean)));
            gating_threshold_mean =
                (F16(VocAlgorithm_GATING_THRESHOLD) +
                 (fix16_mul(
                     F16((VocAlgorithm_GATING_THRESHOLD_INITIAL -
                          VocAlgorithm_GATING_THRESHOLD)),
                     VocAlgorithm__mean_variance_estimator___sigmoid__process(
                        algoParams, algoParams.m_Mean_Variance_Estimator___Uptime_Gating))));
            VocAlgorithm__mean_variance_estimator___sigmoid__set_parameters(
               algoParams, F16(1), gating_threshold_mean,
        F16(VocAlgorithm_GATING_THRESHOLD_TRANSITION));
            sigmoid_gating_mean =
                VocAlgorithm__mean_variance_estimator___sigmoid__process(
                   algoParams, voc_index_from_prior);
            algoParams.m_Mean_Variance_Estimator__Gamma_Mean =
                (fix16_mul(sigmoid_gating_mean, gamma_mean));
            VocAlgorithm__mean_variance_estimator___sigmoid__set_parameters(
               algoParams, F16(1), F16(VocAlgorithm_INIT_DURATION_VARIANCE),
        F16(VocAlgorithm_INIT_TRANSITION_VARIANCE));
            sigmoid_gamma_variance =
                VocAlgorithm__mean_variance_estimator___sigmoid__process(
                   algoParams, algoParams.m_Mean_Variance_Estimator___Uptime_Gamma);
            gamma_variance =
                (algoParams.m_Mean_Variance_Estimator___Gamma +
                 (fix16_mul(
                     (algoParams.m_Mean_Variance_Estimator___Gamma_Initial_Variance -
                      algoParams.m_Mean_Variance_Estimator___Gamma),
                     (sigmoid_gamma_variance - sigmoid_gamma_mean))));
            gating_threshold_variance =
                (F16(VocAlgorithm_GATING_THRESHOLD) +
                 (fix16_mul(
                     F16((VocAlgorithm_GATING_THRESHOLD_INITIAL -
                          VocAlgorithm_GATING_THRESHOLD)),
                     VocAlgorithm__mean_variance_estimator___sigmoid__process(
                        algoParams, algoParams.m_Mean_Variance_Estimator___Uptime_Gating))));
            VocAlgorithm__mean_variance_estimator___sigmoid__set_parameters(
               algoParams, F16(1), gating_threshold_variance,
        F16(VocAlgorithm_GATING_THRESHOLD_TRANSITION));
            sigmoid_gating_variance =
                VocAlgorithm__mean_variance_estimator___sigmoid__process(
                   algoParams, voc_index_from_prior);
            algoParams.m_Mean_Variance_Estimator__Gamma_Variance =
                (fix16_mul(sigmoid_gating_variance, gamma_variance));
            algoParams.m_Mean_Variance_Estimator___Gating_Duration_Minutes =
                (algoParams.m_Mean_Variance_Estimator___Gating_Duration_Minutes +
                 (fix16_mul(F16((VocAlgorithm_SAMPLING_INTERVAL / 60)),
                            ((fix16_mul((F16(1) - sigmoid_gating_mean),
                                        F16((1 + VocAlgorithm_GATING_MAX_RATIO)))) -
                             F16(VocAlgorithm_GATING_MAX_RATIO)))));
            if ((algoParams.m_Mean_Variance_Estimator___Gating_Duration_Minutes <
                 F16(0)))
            {
                algoParams.m_Mean_Variance_Estimator___Gating_Duration_Minutes = F16(0);
            }
            if ((algoParams.m_Mean_Variance_Estimator___Gating_Duration_Minutes >
                 algoParams.m_Mean_Variance_Estimator__Gating_Max_Duration_Minutes))
            {
                algoParams.m_Mean_Variance_Estimator___Uptime_Gating = F16(0);
            }
        }

        static void VocAlgorithm__mean_variance_estimator__process(
            VocAlgorithmParams algoParams, fix16_t sraw, fix16_t voc_index_from_prior)
        {

            fix16_t delta_sgp;
            fix16_t c;
            fix16_t additional_scaling;

            if ((algoParams.m_Mean_Variance_Estimator___Initialized == false))
            {
                algoParams.m_Mean_Variance_Estimator___Initialized = true;
                algoParams.m_Mean_Variance_Estimator___Sraw_Offset = sraw;
                algoParams.m_Mean_Variance_Estimator___Mean = F16(0);
            }
            else
            {
                if (((algoParams.m_Mean_Variance_Estimator___Mean >= F16(100)) ||
                     (algoParams.m_Mean_Variance_Estimator___Mean <= F16(-100))))
                {
                    algoParams.m_Mean_Variance_Estimator___Sraw_Offset =
                        (algoParams.m_Mean_Variance_Estimator___Sraw_Offset +
                                 algoParams.m_Mean_Variance_Estimator___Mean);
                    algoParams.m_Mean_Variance_Estimator___Mean = F16(0);
                }
                sraw = (sraw - algoParams.m_Mean_Variance_Estimator___Sraw_Offset);
                VocAlgorithm__mean_variance_estimator___calculate_gamma(
                   algoParams, voc_index_from_prior);
                delta_sgp = (fix16_div(
                    (sraw - algoParams.m_Mean_Variance_Estimator___Mean),
                    F16(VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING)));
                if ((delta_sgp < F16(0)))
                {
                    c = (algoParams.m_Mean_Variance_Estimator___Std - delta_sgp);
                }
                else
                {
                    c = (algoParams.m_Mean_Variance_Estimator___Std + delta_sgp);
                }
                additional_scaling = F16(1);
                if ((c > F16(1440)))
                {
                    additional_scaling = F16(4);
                }
                algoParams.m_Mean_Variance_Estimator___Std = (fix16_mul(
                    fix16_sqrt((fix16_mul(
                        additional_scaling,
                        (F16(VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING) -
                         algoParams.m_Mean_Variance_Estimator__Gamma_Variance)))),
                    fix16_sqrt((
                        (fix16_mul(
                            algoParams.m_Mean_Variance_Estimator___Std,
                            (fix16_div(
                                algoParams.m_Mean_Variance_Estimator___Std,
                                (fix16_mul(
                                    F16(VocAlgorithm_MEAN_VARIANCE_ESTIMATOR__GAMMA_SCALING),
                                    additional_scaling)))))) +
                        (fix16_mul(
                            (fix16_div(
                                (fix16_mul(
                                    algoParams.m_Mean_Variance_Estimator__Gamma_Variance,
                                    delta_sgp)),
                                additional_scaling)),
                            delta_sgp))))));
                algoParams.m_Mean_Variance_Estimator___Mean =
                    (algoParams.m_Mean_Variance_Estimator___Mean +
                     (fix16_mul(algoParams.m_Mean_Variance_Estimator__Gamma_Mean,
                                delta_sgp)));
            }
        }

        static void VocAlgorithm__mean_variance_estimator___sigmoid__init(
            VocAlgorithmParams algoParams)
        {

            VocAlgorithm__mean_variance_estimator___sigmoid__set_parameters(
               algoParams, F16(0), F16(0), F16(0));
        }

        static void VocAlgorithm__mean_variance_estimator___sigmoid__set_parameters(
            VocAlgorithmParams algoParams, fix16_t L, fix16_t X0, fix16_t K)
        {

            algoParams.m_Mean_Variance_Estimator___Sigmoid__L = L;
            algoParams.m_Mean_Variance_Estimator___Sigmoid__K = K;
            algoParams.m_Mean_Variance_Estimator___Sigmoid__X0 = X0;
        }

        static fix16_t VocAlgorithm__mean_variance_estimator___sigmoid__process(
            VocAlgorithmParams algoParams, fix16_t sample)
        {

            fix16_t x;

            x = (fix16_mul(algoParams.m_Mean_Variance_Estimator___Sigmoid__K,
                           (sample - algoParams.m_Mean_Variance_Estimator___Sigmoid__X0)));
            if ((x < F16(-50)))
            {
                return algoParams.m_Mean_Variance_Estimator___Sigmoid__L;
            }
            else if ((x > F16(50)))
            {
                return F16(0);
            }
            else
            {
                return (fix16_div(algoParams.m_Mean_Variance_Estimator___Sigmoid__L,
                                  (F16(1) + fix16_exp(x))));
            }
        }

        static void VocAlgorithm__mox_model__init(VocAlgorithmParams algoParams)
        {

            VocAlgorithm__mox_model__set_parameters(algoParams, F16(1), F16(0));
        }

        static void VocAlgorithm__mox_model__set_parameters(VocAlgorithmParams algoParams,
                                                            fix16_t SRAW_STD,
                                                            fix16_t SRAW_MEAN)
        {

            algoParams.m_Mox_Model__Sraw_Std = SRAW_STD;
            algoParams.m_Mox_Model__Sraw_Mean = SRAW_MEAN;
        }

        static fix16_t VocAlgorithm__mox_model__process(VocAlgorithmParams algoParams,
                                                        fix16_t sraw)
        {

            return (fix16_mul((fix16_div((sraw - algoParams.m_Mox_Model__Sraw_Mean),
                                         (-(algoParams.m_Mox_Model__Sraw_Std +
                                            F16(VocAlgorithm_SRAW_STD_BONUS))))),
                      F16(VocAlgorithm_VOC_INDEX_GAIN)));
        }

        static void VocAlgorithm__sigmoid_scaled__init(VocAlgorithmParams algoParams)
        {

            VocAlgorithm__sigmoid_scaled__set_parameters(algoParams, F16(0));
        }

        static void
        VocAlgorithm__sigmoid_scaled__set_parameters(VocAlgorithmParams algoParams,
                                                     fix16_t offset)
        {

            algoParams.m_Sigmoid_Scaled__Offset = offset;
        }

        static fix16_t VocAlgorithm__sigmoid_scaled__process(VocAlgorithmParams algoParams,
                                                             fix16_t sample)
        {

            fix16_t x;
            fix16_t shift;

            x = (fix16_mul(F16(VocAlgorithm_SIGMOID_K),
                           (sample - F16(VocAlgorithm_SIGMOID_X0))));
            if ((x < F16(-50)))
            {
                return F16(VocAlgorithm_SIGMOID_L);
            }
            else if ((x > F16(50)))
            {
                return F16(0);
            }
            else
            {
                if ((sample >= F16(0)))
                {
                    shift = (fix16_div(
                        (F16(VocAlgorithm_SIGMOID_L) -
                         (fix16_mul(F16(5), algoParams.m_Sigmoid_Scaled__Offset))),
                        F16(4)));
                    return ((fix16_div((F16(VocAlgorithm_SIGMOID_L) + shift),
                                       (F16(1) + fix16_exp(x)))) -
                            shift);
                }
                else
                {
                    return (fix16_mul(
                        (fix16_div(algoParams.m_Sigmoid_Scaled__Offset,
                                   F16(VocAlgorithm_VOC_INDEX_OFFSET_DEFAULT))),
                        (fix16_div(F16(VocAlgorithm_SIGMOID_L),
                                   (F16(1) + fix16_exp(x))))));
                }
            }
        }

        static void VocAlgorithm__adaptive_lowpass__init(VocAlgorithmParams algoParams)
        {

            VocAlgorithm__adaptive_lowpass__set_parameters(algoParams);
        }

        static void
        VocAlgorithm__adaptive_lowpass__set_parameters(VocAlgorithmParams algoParams)
        {

            algoParams.m_Adaptive_Lowpass__A1 =
                F16((VocAlgorithm_SAMPLING_INTERVAL /
                     (VocAlgorithm_LP_TAU_FAST + VocAlgorithm_SAMPLING_INTERVAL)));
            algoParams.m_Adaptive_Lowpass__A2 =
                F16((VocAlgorithm_SAMPLING_INTERVAL /
                     (VocAlgorithm_LP_TAU_SLOW + VocAlgorithm_SAMPLING_INTERVAL)));
            algoParams.m_Adaptive_Lowpass___Initialized = false;
        }

        static fix16_t VocAlgorithm__adaptive_lowpass__process(VocAlgorithmParams algoParams,
                                                fix16_t sample)
        {

            fix16_t abs_delta;
            fix16_t F1;
            fix16_t tau_a;
            fix16_t a3;

            if ((algoParams.m_Adaptive_Lowpass___Initialized == false))
            {
                algoParams.m_Adaptive_Lowpass___X1 = sample;
                algoParams.m_Adaptive_Lowpass___X2 = sample;
                algoParams.m_Adaptive_Lowpass___X3 = sample;
                algoParams.m_Adaptive_Lowpass___Initialized = true;
            }
            algoParams.m_Adaptive_Lowpass___X1 =
                ((fix16_mul((F16(1) - algoParams.m_Adaptive_Lowpass__A1),
                                    algoParams.m_Adaptive_Lowpass___X1)) +
                         (fix16_mul(algoParams.m_Adaptive_Lowpass__A1, sample)));
            algoParams.m_Adaptive_Lowpass___X2 =
                ((fix16_mul((F16(1) - algoParams.m_Adaptive_Lowpass__A2),
                                    algoParams.m_Adaptive_Lowpass___X2)) +
                         (fix16_mul(algoParams.m_Adaptive_Lowpass__A2, sample)));
            abs_delta =
                (algoParams.m_Adaptive_Lowpass___X1 - algoParams.m_Adaptive_Lowpass___X2);
            if ((abs_delta < F16(0)))
            {
                abs_delta = (-abs_delta);
            }
            F1 = fix16_exp((fix16_mul(F16(VocAlgorithm_LP_ALPHA), abs_delta)));
            tau_a =
                ((fix16_mul(F16((VocAlgorithm_LP_TAU_SLOW - VocAlgorithm_LP_TAU_FAST)),
                            F1)) +
                 F16(VocAlgorithm_LP_TAU_FAST));
            a3 = (fix16_div(F16(VocAlgorithm_SAMPLING_INTERVAL),
                            (F16(VocAlgorithm_SAMPLING_INTERVAL) + tau_a)));
            algoParams.m_Adaptive_Lowpass___X3 =
                ((fix16_mul((F16(1) - a3), algoParams.m_Adaptive_Lowpass___X3)) +
                 (fix16_mul(a3, sample)));
            return algoParams.m_Adaptive_Lowpass___X3;
        }
    }
}
