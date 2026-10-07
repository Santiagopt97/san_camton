import React from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import { SesionProvider } from './SesionContext.jsx'
import 'hotel-ui/theme.css'
import './styles.css'

createRoot(document.getElementById('root')).render(
  <BrowserRouter><SesionProvider><App /></SesionProvider></BrowserRouter>
)
