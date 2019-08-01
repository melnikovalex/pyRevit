"""python engine compatibility module.

Example:
    >>> from pyrevit.compat import IRONPY277
    >>> from pyrevit.compat import safe_strtype
"""

import sys

PY2 = sys.version_info[0] == 2
PY3 = sys.version_info[0] == 3
IRONPY273 = sys.version_info[:3] == (2, 7, 3)
IRONPY277 = sys.version_info[:3] == (2, 7, 7)
IRONPY279 = sys.version_info[:3] == (2, 7, 9)


#pylint: disable=C0103
safe_strtype = str

if PY2:
    # https://gist.github.com/gornostal/1f123aaf838506038710
    safe_strtype = lambda x: unicode(x)  #pylint: disable=E0602,unnecessary-lambda
