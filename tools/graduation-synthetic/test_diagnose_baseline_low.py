"""Offline unit tests for the existing-run diagnostic; no API calls."""
import unittest

from diagnose_baseline_low import margin, reference_digest


class BaselineDiagnosisTests(unittest.TestCase):
    def test_below_lower_margin_is_relative_to_lower_bound(self):
        result = margin(448230.732, 500496.85, 596353.22)
        self.assertEqual(result["belowLowerPercent"], 10.443)
        self.assertEqual(result["aboveUpperPercent"], 0)

    def test_above_upper_margin_is_relative_to_upper_bound(self):
        result = margin(200, 100, 150)
        self.assertEqual(result["aboveUpperPercent"], 33.333)
        self.assertEqual(result["belowLowerPercent"], 0)

    def test_cross_structure_reference_is_reported_without_assuming_bug(self):
        metadata = {
            "SYN-00021": {"structure_type": "RC"},
            "SYN-00022": {"structure_type": "SRC"},
        }
        result = reference_digest([
            {"projectName": "SYNTHETIC:SYN-00021 Project A", "similarityScore": 80},
            {"projectName": "SYNTHETIC:SYN-00022 Project B", "similarityScore": 78},
        ], "RC", metadata)
        self.assertEqual(result["crossStructure"], ["SYN-00022"])
        self.assertEqual(result["unresolvedReferenceIds"], [])
        self.assertEqual(result["count"], 2)

    def test_bad_range_refused(self):
        with self.assertRaises(ValueError):
            margin(2, 0, 10)


if __name__ == "__main__":
    unittest.main()
