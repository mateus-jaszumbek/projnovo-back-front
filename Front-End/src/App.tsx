import { Route, Routes } from "react-router-dom";
import { AuthProvider, ProtectedRoute } from "./auth/AuthContext";
import { CookieConsentBanner, CookieConsentProvider } from "./legal/CookieConsent";
import { Shell } from "./layout/Shell";
import { LegalDocumentPage } from "./pages/LegalDocumentPage";
import { AuthPage } from "./pages/AuthPage";
import { AcompanhamentoPublicoPage } from "./pages/AcompanhamentoPublicoPage";

function App() {
  return (
    <AuthProvider>
      <CookieConsentProvider>
        <Routes>
          <Route path="/entrar" element={<AuthPage />} />
          <Route path="/termos-de-uso" element={<LegalDocumentPage documentKey="terms" />} />
          <Route
            path="/privacidade-lgpd"
            element={<LegalDocumentPage documentKey="privacy" />}
          />
          <Route
            path="/politica-de-cookies"
            element={<LegalDocumentPage documentKey="cookies" />}
          />
          <Route path="/acompanhar/:token" element={<AcompanhamentoPublicoPage />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <Shell />
              </ProtectedRoute>
            }
          />
        </Routes>
        <CookieConsentBanner />
      </CookieConsentProvider>
    </AuthProvider>
  );
}

export default App;
