"""Pure offline tests for paired API report; never call an HTTP endpoint."""
import unittest

from audit_paired_anomaly_api import pairs, summarize


class PairAuditTests(unittest.TestCase):
    def test_paired_delta_keeps_same_project_and_existing_baseline_red(self):
        baseline = {
            "scenario": "baseline", "baseProjectId": "SYN-00030",
            "quantities": {"鋼筋": {"observedQuantity": 100.0, "trafficLight": "Red",
                                 "analysisStatus": "Low"}},
            "price": {"observedAmount": 1000.0, "trafficLight": "Red",
                      "analysisStatus": "Low"},
        }
        changed = {
            "scenario": "design_rebar_low", "baseProjectId": "SYN-00030",
            "quantities": {"鋼筋": {"observedQuantity": 40.0, "trafficLight": "Red",
                                 "analysisStatus": "Low"}},
            "price": {"observedAmount": 800.0, "trafficLight": "Red",
                      "analysisStatus": "Low"},
        }
        rows = pairs([baseline, changed])
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["quantities"]["鋼筋"]["observedDelta"], -60.0)
        self.assertEqual(rows[0]["priceObservedDelta"], -200.0)
        self.assertEqual(rows[0]["baselinePriceLight"], "Red")
        self.assertEqual(rows[0]["variantPriceLight"], "Red")

    def test_summary_tracks_only_approved_quantity_categories(self):
        upload = {"analysisCaseId": 10, "analysisBidVersionId": 11, "itemCount": 446}
        quantity = {"analysisRunId": 12, "result": {
            "items": [
                {"detectionCategoryName": "混凝土", "observedQuantity": 123.4,
                 "unitName": "m3", "range": {"lowerQuantity": 100, "upperQuantity": 200},
                 "analysisStatus": "Normal", "trafficLight": "Green"},
                {"detectionCategoryName": "鋼骨", "observedQuantity": 4,
                 "unitName": "kg", "trafficLight": "Green"},
            ],
        }}
        price = {"analysisRunId": 13, "result": {
            "observedAmount": 500, "lowerAmount": 400, "upperAmount": 600,
            "analysisStatus": "Normal", "trafficLight": "Green",
            "referenceProjectCount": 8,
        }}
        value = summarize("baseline", "SYN-00030", upload, quantity, price)
        self.assertEqual(value["parsedItemCount"], 446)
        self.assertEqual(set(value["quantities"]), {"混凝土"})
        self.assertEqual(value["quantityRunId"], 12)
        self.assertEqual(value["priceRunId"], 13)


if __name__ == "__main__":
    unittest.main()
