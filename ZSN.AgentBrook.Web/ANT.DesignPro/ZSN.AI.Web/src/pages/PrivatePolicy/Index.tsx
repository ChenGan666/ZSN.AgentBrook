import { PageContainer } from '@ant-design/pro-components';
import { Footer } from '@/components';
import { Card } from 'antd';

import { FormattedMessage, SelectLang } from '@umijs/max';
import { createStyles } from 'antd-style';

const useStyles = createStyles(({ token }) => {
  return {
    action: {
      marginLeft: '8px',
      color: 'rgba(0, 0, 0, 0.2)',
      fontSize: '24px',
      verticalAlign: 'middle',
      cursor: 'pointer',
      transition: 'color 0.3s',
      '&:hover': {
        color: token.colorPrimaryActive,
      },
    },
    lang: {
      width: 42,
      height: 42,
      lineHeight: '42px',
      position: 'fixed',
      right: 16,
      borderRadius: token.borderRadius,
      ':hover': {
        backgroundColor: token.colorBgTextHover,
      },
    },
    container: {
      display: 'flex',
      flexDirection: 'column',
      height: '100vh',
      overflow: 'auto',
      backgroundImage:
        "url('https://mdn.alipayobjects.com/yuyan_qk0oxh/afts/img/V-_oS6r-i7wAAAAAAAAAAAAAFl94AQBr')",
      backgroundSize: '100% 100%',
    },
  };
});

const Lang = () => {
  const { styles } = useStyles();

  return (
    <div className={styles.lang} data-lang>
      {SelectLang && <SelectLang />}
    </div>
  );
};

const Index: React.FC = () => {

  return (
    <PageContainer>
      <Card
        style={{
          borderRadius: 8,
        }}
        bodyStyle={{
          backgroundImage: 'background-image: linear-gradient(75deg, #1A1B1F 0%, #191C1F 100%)'
        }}
      >
<Lang></Lang>
        <FormattedMessage
          key="privatePolicy"
          id="privatePolicy.data"
          defaultMessage="隐私政策"
          values={{
            h1: (chunks) => <h1>{chunks}</h1>,
            h2: (chunks) => <h2>{chunks}</h2>,
            h3: (chunks) => <h3>{chunks}</h3>,
            p: (chunks) => <p>{chunks}</p>,
            ul: (chunks) => <ul>{chunks}</ul>,
            li: (chunks) => <li>{chunks}</li>,
          }}
        />
      </Card>
      <Footer></Footer>
    </PageContainer>

  );
};


export default Index;
