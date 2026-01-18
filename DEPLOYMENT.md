# הוראות העלאה לאונליין

## אפשרות 1: Railway.app (מומלץ - הכי פשוט)

### שלב 1: יצירת חשבון
1. היכנס ל-[railway.app](https://railway.app)
2. התחבר עם חשבון GitHub

### שלב 2: העלאת הפרויקט ל-GitHub
```bash
# צור ריפו חדש ב-GitHub ואז:
git remote add origin https://github.com/YOUR_USERNAME/TicTacToeGame.git
git branch -M main
git push -u origin main
```

### שלב 3: Deploy ב-Railway
1. לחץ על "New Project"
2. בחר "Deploy from GitHub repo"
3. בחר את הריפו `TicTacToeGame`
4. Railway יזהה את ה-Dockerfile אוטומטית
5. המתן לבנייה (~2-3 דקות)
6. לחץ על "Generate Domain" לקבלת כתובת URL

**עלות**: $5 קרדיט חינם לחודש (מספיק בהחלט)

---

## אפשרות 2: Render.com

### שלב 1: יצירת חשבון
1. היכנס ל-[render.com](https://render.com)
2. התחבר עם GitHub

### שלב 2: יצירת Web Service
1. לחץ "New" → "Web Service"
2. חבר את הריפו מ-GitHub
3. הגדרות:
   - **Environment**: Docker
   - **Plan**: Free
4. לחץ "Create Web Service"

**חינמי לגמרי** (עם "שינה" אחרי 15 דקות)

---

## אפשרות 3: Azure App Service

### דרישות
- חשבון Azure (ניתן ליצור חינם עם $200 קרדיט)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

### שלבים
```bash
# התחברות
az login

# יצירת Resource Group
az group create --name TicTacToeRG --location westeurope

# יצירת App Service Plan (חינמי)
az appservice plan create --name TicTacToePlan --resource-group TicTacToeRG --sku F1 --is-linux

# יצירת Web App
az webapp create --resource-group TicTacToeRG --plan TicTacToePlan --name tictactoe-game-il --runtime "DOTNETCORE:8.0"

# Deploy
az webapp deployment source config-local-git --name tictactoe-game-il --resource-group TicTacToeRG

# קבל את ה-URL ל-push
# ואז:
git remote add azure <DEPLOYMENT_URL>
git push azure main
```

---

## אפשרות 4: Fly.io

### התקנה
```bash
# התקנת flyctl
powershell -Command "iwr https://fly.io/install.ps1 -useb | iex"

# התחברות
fly auth login

# יצירת אפליקציה
fly launch

# Deploy
fly deploy
```

---

## בדיקה מקומית עם Docker

```bash
# בניית התמונה
docker build -t tictactoe .

# הרצה
docker run -p 8080:8080 tictactoe

# פתח בדפדפן: http://localhost:8080
```

---

## טיפים

1. **בעיות SSL**: האפליקציה כבר מוגדרת לעבוד ללא HTTPS redirection בפרודקשן
2. **Session**: המשחק משתמש ב-Session, שעובד מצוין בכל הפלטפורמות
3. **זמני תגובה**: בתוכניות חינמיות, הטעינה הראשונה עלולה לקחת 10-30 שניות אם האפליקציה "ישנה"
