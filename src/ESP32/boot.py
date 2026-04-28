# boot.py - import paths setup (main.py runs afterwards in MicroPython)
import gc
import sys

gc.collect()

for _p in ("lib", "/flash/lib", "src", "/flash/src", "/src", ".", ""):
    if _p not in sys.path:
        sys.path.insert(0, _p)
