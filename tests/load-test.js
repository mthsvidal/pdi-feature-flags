import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { randomString } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';
import fs from 'k6/x/file';

const API_BASE_URL = 'http://localhost:5000';
const CONTRACT_FILE = 'tests/contract.json';

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.1'],
  },
};

export default function () {
  group('Contract Processing Flow', () => {
    // 1) Ler o arquivo de teste
    const contractData = JSON.parse(open(CONTRACT_FILE));
    
    // 2) Gerar idempotencyKey único
    const idempotencyKey = `k6-test-${randomString(8)}-${Date.now()}`;
    
    // 3) Preparar payload com idempotencyKey
    const payload = {
      idempotencyKey: idempotencyKey,
      contracts: contractData.contracts
    };

    // 4) Fazer POST no endpoint
    let response = http.post(
      `${API_BASE_URL}/api/contracts`,
      JSON.stringify(payload),
      {
        headers: {
          'Content-Type': 'application/json',
        },
        timeout: '30s',
      }
    );

    // 5) Validar resposta
    check(response, {
      'status is 202': (r) => r.status === 202,
      'response has processingDetails': (r) => r.json('processingDetails') !== undefined,
      'response has summary': (r) => r.json('processingDetails.summary') !== undefined,
      'summary has totalContractSpecifications': (r) => r.json('processingDetails.summary.totalContractSpecifications') !== null,
      'summary has specificationsByAssetHolder': (r) => r.json('processingDetails.summary.specificationsByAssetHolder') !== undefined,
    }, { name: 'Response Validation' });

    // 6) Extrair dados da resposta
    const responseBody = response.json('processingDetails.summary');
    const specificationsByAssetHolder = responseBody['specificationsByAssetHolder'] || [];

    console.log(`\n=== Contract Processing Summary ===`);
    console.log(`IdempotencyKey: ${idempotencyKey}`);
    console.log(`Total Specifications: ${responseBody['totalContractSpecifications']}`);
    console.log(`Total Notified: ${responseBody['totalNotifiedContractSpecifications']}`);
    console.log(`Asset Holders: ${specificationsByAssetHolder.length}`);

    // 7) Verificar arquivos para cada asset holder notificado
    let pathsFound = 0;
    let pathsNotFound = 0;

    specificationsByAssetHolder.forEach((holder, index) => {
      const notificationPath = holder['notificationPath'];
      const originalAssetHolder = holder['originalAssetHolder'];
      const count = holder['count'];

      if (notificationPath) {
        console.log(`\n--- Asset Holder ${index + 1}: ${originalAssetHolder} ---`);
        console.log(`Notification Path: ${notificationPath}`);
        console.log(`Count: ${count}`);

        // Verificar se arquivo existe usando comando do sistema
        const result = checkFileExists(notificationPath);
        
        if (result.exists) {
          console.log(`✓ File found: ${notificationPath}`);
          pathsFound++;
          
          // Tentar ler conteúdo do arquivo
          try {
            const fileContent = open(notificationPath);
            const parsedContent = JSON.parse(fileContent);
            check(parsedContent, {
              'file has IdempotencyKey': (f) => f.IdempotencyKey === idempotencyKey,
              'file has OriginalAssetHolder': (f) => f.OriginalAssetHolder === originalAssetHolder,
              'file has ContractSpecifications': (f) => Array.isArray(f.ContractSpecifications) && f.ContractSpecifications.length > 0,
            }, { name: `File Content Validation (${originalAssetHolder})` });

            console.log(`  - Specifications in file: ${parsedContent.ContractSpecifications.length}`);
          } catch (e) {
            console.log(`✗ Failed to parse file content: ${e}`);
          }
        } else {
          console.log(`✗ File NOT found: ${notificationPath}`);
          pathsNotFound++;
        }
      }
    });

    console.log(`\n=== Summary ===`);
    console.log(`Paths Found: ${pathsFound}/${specificationsByAssetHolder.length}`);
    console.log(`Paths Not Found: ${pathsNotFound}/${specificationsByAssetHolder.length}`);

    check(
      { pathsFound, pathsNotFound },
      {
        'all notification paths exist': (s) => s.pathsNotFound === 0,
      },
      { name: 'File Existence Validation' }
    );

    sleep(1);
  });
}

/**
 * Verifica se arquivo existe no disco
 * Usa exec para rodar comando do sistema
 */
function checkFileExists(filePath) {
  // Para Windows, usar comando: if exist
  const sanitizedPath = filePath.replace(/\//g, '\\');
  
  try {
    // Tentar ler o arquivo usando open()
    const content = open(filePath);
    return { exists: true, content };
  } catch (e) {
    return { exists: false, error: e.message };
  }
}
