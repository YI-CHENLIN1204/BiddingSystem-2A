"""Offline fidelity probes for the external report, sections 3.4–3.7."""
import json
from math import exp, erf, sqrt
from statistics import mean

WEIGHTS = (.30, .15, .07, .11, .07, .05, .20, .05)
GRID = 4096
TOLERANCE = 1e-8


def distance(a, b):
    relative = lambda i: min(1, abs(a[i] - b[i]) / max(a[i], 1))
    deltas = (relative(0), int(a[1] != b[1]), int(a[2] != b[2]),
              relative(3), relative(4), relative(5), int(a[6] != b[6]),
              min(1, abs(a[7] - b[7]) / 10))
    return 100 * sum(w * d for w, d in zip(WEIGHTS, deltas))


def critical_bandwidth(values):
    span = max(values) - min(values)
    if span == 0:
        return None
    samples = [(v - min(values)) / span for v in values]
    points = sorted(set(samples + [i / GRID for i in range(GRID + 1)]))

    def modes(h):
        pos = False
        count = 0
        for x in points:
            slope = sum((v - x) * exp(-0.5 * ((v - x) / h) ** 2) for v in samples)
            if slope > 0:
                pos = True
            elif slope < 0 and pos:
                count += 1
                pos = False
        return count

    lo, hi = 1e-4, 1.0
    while hi - lo > TOLERANCE:
        mid = (lo + hi) / 2
        if modes(mid) == 1:
            hi = mid
        else:
            lo = mid
    return hi * span


def quantile(values, bandwidth, probability):
    def cdf(x):
        return mean((1 + erf((x - v) / (bandwidth * sqrt(2)))) / 2 for v in values)
    origin = cdf(0)
    lo, hi = 0, max(values) + 12 * bandwidth
    while hi - lo > 1e-12 * max(1, hi):
        mid = (lo + hi) / 2
        if (cdf(mid) - origin) / (1 - origin) < probability:
            lo = mid
        else:
            hi = mid
    return (lo + hi) / 2


def main():
    a = (10000, "res", "RC", 10, 2, 2000, "city-A", 2020)
    b = (12000, "res", "RC", 12, 1, 2500, "city-B", 2018)
    values = [.04, .05, .06]
    h = critical_bandwidth(values)
    bounds = [10000 * quantile(values, h, p) for p in (.05, .95)]
    price = [2000 * quantile([10000, 12500, 15000], h / .02 * 5000, p) for p in (.05, .95)]
    with_report_h = [10000 * quantile(values, .00485, p) for p in (.05, .95)]
    result = dict(distance=distance(a, b), flat_abstains=critical_bandwidth([5, 5, 5]) is None,
                  normalized_bandwidth=h/.02, bounds=bounds, price=price,
                  bounds_using_report_bandwidth=with_report_h,
                  grid=GRID, bandwidth_start=[1e-4, 1.0], bandwidth_tolerance=TOLERANCE,
                  cdf_relative_tolerance=1e-12, cdf_upper_tail_h=12)
    assert abs(result["distance"] - 33.95) < 1e-9 and result["flat_abstains"]
    assert max(abs(x-y) for x,y in zip(with_report_h, (349.54, 650.46))) < 0.1
    assert abs(h / .02 - .2425) < .0001
    assert max(abs(x-y) for x,y in zip(bounds, (349.54, 650.46))) < .1
    assert max(abs(x-y) for x,y in zip(price, (17476839, 32523161))) < 10
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
