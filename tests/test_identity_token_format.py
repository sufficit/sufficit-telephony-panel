import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("token_format", Path(__file__).parents[1] / "deploy/identity-token-format.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TokenFormatTests(unittest.TestCase):
    def test_exact_overlay_and_preservation(self):
        original = {"unrelated": {"value": 5}, "Sufficit": {"Identity": {"Tokens": {
            "UseReferenceAccessTokens": True,
            "AccessTokenFormatsByClient": {"SufficitBlazorServer": "Jwt"},
            "AccessTokenFormatsByResource": {"existing-resource": "Jwt"}}}}}
        updated = module.merge(original)
        self.assertEqual(updated["Sufficit"]["Identity"]["Tokens"]["AccessTokenFormatsByClient"][module.CLIENT], "Jwt")
        self.assertEqual(module.merge(updated, rollback=True), original)
        self.assertEqual(module.merge(updated), updated)
        overlay = json.loads((Path(__file__).parents[1] / "deploy/identity-token-format.json").read_text())
        self.assertEqual(module.merge({}), overlay)

    def test_unexpected_rule_rejected(self):
        original = module.merge({})
        original["Sufficit"]["Identity"]["Tokens"]["AccessTokenFormatsByClient"][module.CLIENT] = "Reference"
        with self.assertRaises(ValueError):
            module.merge(original)

    def test_atomic_write_permissions_and_stale_hash(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "settings.json"
            path.write_text('{"Other": true}')
            path.chmod(0o640)
            before = path.read_bytes()
            digest = hashlib.sha256(before).hexdigest()
            with self.assertRaises(ValueError):
                module.apply(path, "stale")
            self.assertEqual(path.read_bytes(), before)
            updated = module.apply(path, digest)
            self.assertEqual(path.stat().st_mode & 0o777, 0o640)
            self.assertEqual(module.apply(path, updated), updated)
            self.assertTrue(json.loads(path.read_text())["Other"])
            link = Path(folder) / "link.json"
            link.symlink_to(path)
            with self.assertRaises(ValueError):
                module.apply(link, updated)


if __name__ == "__main__":
    unittest.main()
