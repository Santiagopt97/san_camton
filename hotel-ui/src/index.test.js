import * as ui from './index.js'

const PUBLICOS = [
  'Button', 'Card', 'Alert', 'Badge', 'Spinner', 'Toolbar', 'DataTable', 'Pager', 'AppShell',
  'RequirePerfil', 'Modal', 'ConfirmModal', 'Field', 'Input', 'Textarea', 'Select', 'Checkbox',
  'FormGrid', 'FormActions', 'createSession', 'useDebouncedEffect', 'useLoading', 'validar', 'fetchSeguro', 'Carousel', 'ImageManager',
]

test('el paquete exporta su API pública', () => {
  for (const nombre of PUBLICOS) expect(ui[nombre], nombre).toBeDefined()
})
